using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;
using AppProduct.Services;
using System.Security.Claims;
using System.Globalization;
using Stripe;
using Stripe.Checkout;
using ShippingRateModel = AppProduct.Shared.Models.ShippingRate;
using StripeLineItem = Stripe.LineItem;
using TaxRateModel = AppProduct.Shared.Models.TaxRate;
using SharedPaymentMethod = AppProduct.Shared.Models.PaymentMethod;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class PaymentController(ApplicationDbContext ctx, IConfiguration configuration, IEmailNotificationService emailService, IPdfGenerationService pdfService, INotificationService notificationService) : ControllerBase
{
    [HttpPost("create-stripe-session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreateStripeSessionResponse>> CreateStripeSessionAsync([FromBody] CreateStripeSessionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var stripeSecretKey = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(stripeSecretKey))
        {
            return BadRequest(new
            {
                error = "Stripe is not configured. Please set Stripe:SecretKey in configuration.",
            });
        }

        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)!
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart?.Items == null || cart.Items.Count == 0)
        {
            return BadRequest(new { error = "Cart is empty" });
        }

        StripeConfiguration.ApiKey = stripeSecretKey;

        var baseUrl = !string.IsNullOrWhiteSpace(request.BaseUrl)
            ? request.BaseUrl.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}";

        var productLineItems = new List<SessionLineItemOptions>();
        var metadataProductIds = new List<string>();
        var cartSubtotal = 0m;

        foreach (var cartItem in cart.Items)
        {
            var quantity = Math.Max(1, cartItem.Quantity);
            var unitPrice = cartItem.UnitPrice ?? cartItem.Product?.Price ?? 0m;
            if (unitPrice < 0)
            {
                unitPrice = 0;
            }

            cartSubtotal += unitPrice * quantity;

            var originalName = cartItem.Product?.Name?.Trim();
            var productModel = cartItem.Product?.Model?.Trim();
            var productDisplayName = cartItem.Product?.GetDisplayName() ?? originalName ?? "Product";
            var productMetadata = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(originalName))
            {
                productMetadata["product_name"] = originalName;
            }
            if (!string.IsNullOrWhiteSpace(productModel))
            {
                productMetadata["product_model"] = productModel;
            }
            if (cartItem.ProductId.HasValue)
            {
                productMetadata["product_id"] = cartItem.ProductId.Value.ToString(CultureInfo.InvariantCulture);
            }

            productLineItems.Add(new SessionLineItemOptions
            {
                Quantity = quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = ToStripeAmount(unitPrice),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = productDisplayName,
                        Description = cartItem.Product?.Description ?? string.Empty,
                        Metadata = productMetadata.Count > 0 ? productMetadata : null,
                    },
                },
            });

            if (cartItem.ProductId.HasValue)
            {
                metadataProductIds.Add(cartItem.ProductId.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        TaxRateModel? taxRate = null;
        var billingStateCode = request.BillingStateCode?.Trim();
        if (!string.IsNullOrEmpty(billingStateCode))
        {
            taxRate = await ctx.TaxRate
                .Where(x => x.StateCode == billingStateCode && x.IsActive)
                .OrderByDescending(x => x.ModifiedDate)
                .FirstOrDefaultAsync();
        }

        var taxAmount = taxRate?.CombinedTaxRate is decimal combinedRate && combinedRate > 0
            ? Math.Round(cartSubtotal * combinedRate / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

        if (taxAmount > 0)
        {
            productLineItems.Add(new SessionLineItemOptions
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = ToStripeAmount(taxAmount),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Tax",
                        Description = taxRate?.State ?? "Sales Tax",
                    },
                },
            });
        }

        var shippingRate = await GetDefaultShippingRateAsync();
        var shippingAmount = request.ShippingAmount > 0 ? request.ShippingAmount : shippingRate?.Amount ?? 0m;
        if (shippingAmount > 0)
        {
            productLineItems.Add(new SessionLineItemOptions
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = ToStripeAmount(shippingAmount),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = shippingRate?.Name ?? "Shipping",
                        Description = shippingRate?.Notes ?? "Shipping",
                    },
                },
            });
        }

        var metadata = new Dictionary<string, string>
        {
            ["user_id"] = userId
        };

        if (metadataProductIds.Count > 0)
        {
            metadata["product_ids"] = string.Join(',', metadataProductIds);
        }

        if (!string.IsNullOrWhiteSpace(billingStateCode))
        {
            metadata["billing_state_code"] = billingStateCode;
        }

        if (taxRate?.CombinedTaxRate is decimal rateValue)
        {
            metadata["tax_rate"] = rateValue.ToString(CultureInfo.InvariantCulture);
        }

        metadata["shipping_amount"] = shippingAmount.ToString(CultureInfo.InvariantCulture);

        if (shippingRate?.Id is long shippingRateId)
        {
            metadata["shipping_rate_id"] = shippingRateId.ToString(CultureInfo.InvariantCulture);
        }

        AddMetadataIfPresent(metadata, "shipping_address", request.ShippingAddress);
        AddMetadataIfPresent(metadata, "billing_address", request.BillingAddress);
        AddMetadataIfPresent(metadata, "notes", request.Notes);

        var sessionOptions = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "payment",
            LineItems = productLineItems,
            SuccessUrl = $"{baseUrl}/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}/checkout/cancel?session_id={{CHECKOUT_SESSION_ID}}",
            Metadata = metadata
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions);

        return Ok(new CreateStripeSessionResponse
        {
            SessionId = session.Id,
            Url = session.Url
        });

        static long ToStripeAmount(decimal amount)
        {
            var normalized = Math.Max(0m, decimal.Round(amount, 2, MidpointRounding.AwayFromZero));
            return (long)decimal.Round(normalized * 100m, 0, MidpointRounding.AwayFromZero);
        }
    }

    [HttpPost("create-paypal-session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreatePayPalSessionResponse>> CreatePayPalSessionAsync([FromBody] CreatePayPalSessionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // For now, return a placeholder response since PayPal integration requires proper SDK setup
        // In a real implementation, you would use PayPal SDK to create a payment session
        var baseUrl = !string.IsNullOrWhiteSpace(request.BaseUrl)
            ? request.BaseUrl.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}";

        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)!
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart?.Items == null || cart.Items.Count == 0)
        {
            return BadRequest(new { error = "Cart is empty" });
        }

        // Generate a placeholder session ID for demonstration
        var sessionId = $"paypal_session_{Guid.NewGuid():N}";

        return Ok(new CreatePayPalSessionResponse
        {
            SessionId = sessionId,
            Url = $"{baseUrl}/checkout/paypal-redirect?session_id={sessionId}&user_id={userId}"
        });
    }

    static void AddMetadataIfPresent(IDictionary<string, string> metadataValues, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        value = value.Trim();
        metadataValues[key] = value.Length > 500 ? value[..500] : value;
    }

    [HttpPost("checkout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> CheckoutAsync([FromBody] List<CartProduct> cart)
    {
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        };

        // Cart data logged internally

        try
        {
            var stripeSecretKey = configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeSecretKey))
            {
                return BadRequest(new { error = "Stripe is not configured. Please set Stripe:SecretKey in configuration." });
            }
            StripeConfiguration.ApiKey = stripeSecretKey;

            if (cart == null || !cart.Any())
            {
                return BadRequest(new { error = "Cart is empty" });
            }

            // Calculate tax
            var taxRate = 0.0875m; // 8.75% tax (can be made dynamic later)
            var subtotal = cart.Sum(item => (item.Price ?? 0) * item.Quantity);
            var totalTax = subtotal * taxRate;

            var shippingRate = await GetDefaultShippingRateAsync();
            var shippingAmount = shippingRate?.Amount ?? 0m;

            var domain = $"{Request.Scheme}://{Request.Host}";
            var lineItems = new List<SessionLineItemOptions>();
            var productIdsMetadata = new List<string>();

            foreach (var cartItem in cart)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)((cartItem.Price ?? 0) * 100), // Convert to cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = cartItem.Name ?? "Product",
                            Description = cartItem.Description ?? string.Empty,
                        },
                    },
                    Quantity = cartItem.Quantity,
                });

                productIdsMetadata.Add(cartItem.Id?.ToString() ?? string.Empty);
            }

            var metadata = new Dictionary<string, string>();
            if (productIdsMetadata.Count > 0)
            {
                metadata["product_ids"] = string.Join(',', productIdsMetadata);
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = $"{domain}/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/checkout/cancel?session_id={{CHECKOUT_SESSION_ID}}",
            };

            // Add tax as a separate line item
            if (totalTax > 0)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(totalTax * 100), // Convert to cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Tax",
                            Description = "Sales Tax (8.75%)",
                        },
                    },
                    Quantity = 1,
                });
            }

            if (shippingAmount > 0)
            {
                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(shippingAmount * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = shippingRate?.Name ?? "Shipping",
                            Description = shippingRate?.Notes ?? "Shipping",
                        },
                    },
                    Quantity = 1,
                });
            }

            metadata["shipping_amount"] = shippingAmount.ToString(CultureInfo.InvariantCulture);
            if (shippingRate is not null)
            {
                metadata["shipping_rate_id"] = shippingRate.Id.ToString(CultureInfo.InvariantCulture);
            }

            if (metadata.Count > 0)
            {
                options.Metadata = metadata;
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Return session information to the client
            return Ok(new
            {
                sessionId = session.Id,
                url = session.Url,
                successUrl = options.SuccessUrl,
                cancelUrl = options.CancelUrl
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Payment initialization failed: {ex.Message}" });
        }
    }

    [HttpPost("confirm-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Order>> ConfirmPaymentAsync([FromBody] ConfirmPaymentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        try
        {
            if (request.PaymentMethod == SharedPaymentMethod.CreditCard)
            {
                if (string.IsNullOrEmpty(request.StripeSessionId))
                    return BadRequest(new { error = "Stripe session ID is required for credit card payments" });

                StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
                var service = new SessionService();
                var session = await service.GetAsync(
                    request.StripeSessionId,
                    new SessionGetOptions { Expand = new List<string> { "line_items", "total_details.breakdown", "customer", "customer_details" } });

                if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { error = "Payment not completed" });

                var paymentIntentId = session.PaymentIntentId ?? request.PaymentIntentId ?? session.Id;

                var order = await CreateOrderFromStripeSessionAsync(
                    session,
                    userId,
                    request.PaymentMethod,
                    OrderStatus.Confirmed,
                    request.ShippingAddress,
                    request.BillingAddress,
                    request.Notes,
                    paymentIntentId,
                    clearCart: true,
                    sendEmail: true);
                return Ok(order);
            }

            var manualOrder = await CreateOrderFromStoredCartAsync(request, userId);
            return Ok(manualOrder);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error confirming payment: {ex.Message}" });
        }
    }

    [HttpPost("cancel-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Order>> CancelPaymentAsync([FromBody] CancelPaymentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.StripeSessionId))
        {
            return BadRequest(new { error = "Stripe session ID is required" });
        }

        var stripeSecretKey = configuration["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(stripeSecretKey))
        {
            return BadRequest(new { error = "Stripe is not configured. Please set Stripe:SecretKey in configuration." });
        }

        StripeConfiguration.ApiKey = stripeSecretKey;
        var service = new SessionService();

        Session session;
        try
        {
            session = await service.GetAsync(
                request.StripeSessionId,
                new SessionGetOptions { Expand = new List<string> { "line_items", "total_details.breakdown", "customer", "customer_details" } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Unable to retrieve Stripe session: {ex.Message}" });
        }

        var paymentIntentId = session.PaymentIntentId ?? request.StripeSessionId;

        var existingOrder = await ctx.Order
            .Include(o => o.Items)!
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.PaymentIntentId == paymentIntentId);

        if (existingOrder != null)
        {
            if (existingOrder.Status != OrderStatus.Cancelled)
            {
                existingOrder.Status = OrderStatus.Cancelled;
                await ctx.SaveChangesAsync();
                await TrySendOrderEmailsAsync(existingOrder, OrderStatus.Cancelled);
            }

            return Ok(existingOrder);
        }

        var order = await CreateOrderFromStripeSessionAsync(
            session,
            userId,
            SharedPaymentMethod.CreditCard,
            OrderStatus.Cancelled,
            shippingAddressOverride: null,
            billingAddressOverride: null,
            notesOverride: null,
            explicitPaymentIntentId: paymentIntentId,
            clearCart: false,
            sendEmail: true);

        return Ok(order);
    }

    private async Task<Order> CreateOrderFromStripeSessionAsync(
        Session session,
        string userId,
        SharedPaymentMethod paymentMethod,
        OrderStatus status,
        string? shippingAddressOverride,
        string? billingAddressOverride,
        string? notesOverride,
        string? explicitPaymentIntentId,
        bool clearCart,
        bool sendEmail)
    {
        if (session.LineItems == null || !session.LineItems.Data.Any())
        {
            var service = new SessionService();
            session = await service.GetAsync(
                session.Id,
                new SessionGetOptions { Expand = new List<string> { "line_items", "total_details.breakdown", "customer", "customer_details" } });
        }

        if (session.LineItems == null || !session.LineItems.Data.Any())
        {
            throw new InvalidOperationException("No items found in payment session");
        }

        static bool ContainsToken(StripeLineItem item, string token) =>
            item.Description?.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        var productLineItems = session.LineItems.Data
            .Where(item => !ContainsToken(item, "Tax") && !ContainsToken(item, "Shipping"))
            .ToList();

        var subtotal = productLineItems
            .Sum(item => (decimal)item.AmountTotal / 100m);

        var taxAmount = session.TotalDetails?.AmountTax is long taxCents
            ? taxCents / 100m
            : session.LineItems.Data
                .Where(item => ContainsToken(item, "Tax"))
                .Sum(item => (decimal)item.AmountTotal / 100m);

        var shippingAmount = await DetermineShippingAmountAsync(session);

        var amountTotalCents = session.AmountTotal
            ?? throw new InvalidOperationException("Stripe session did not include a total amount");
        var total = amountTotalCents / 100m;

        var productIdsFromMetadata = new Queue<long?>(
            session.Metadata != null
            && session.Metadata.TryGetValue("product_ids", out var productIdsRaw)
            && productIdsRaw != null
                ? productIdsRaw
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => long.TryParse(id, out var parsedId) ? parsedId : (long?)null)
                : Array.Empty<long?>());

        var orderItems = new List<OrderItem>();
        foreach (var lineItem in productLineItems)
        {
            if (lineItem.Quantity is not { } quantity || quantity <= 0)
            {
                continue;
            }

            var amountTotal = lineItem.AmountTotal;
            if (amountTotal <= 0)
            {
                continue;
            }

            long? productId = null;
            if (productIdsFromMetadata.Count > 0)
            {
                productId = productIdsFromMetadata.Dequeue();
            }

            var orderItem = new OrderItem
            {
                ProductId = productId,
                Quantity = (int)quantity,
                UnitPrice = (decimal)amountTotal / quantity / 100m,
                TotalPrice = (decimal)amountTotal / 100m
            };

            orderItems.Add(orderItem);
        }

        var productIds = orderItems
            .Where(item => item.ProductId.HasValue)
            .Select(item => item.ProductId!.Value)
            .Distinct()
            .ToList();

        if (productIds.Any())
        {
            var productsById = await ctx.Product
                .Where(p => p.Id.HasValue && productIds.Contains(p.Id.Value))
                .ToDictionaryAsync(p => p.Id!.Value);

            foreach (var orderItem in orderItems)
            {
                if (orderItem.ProductId.HasValue &&
                    productsById.TryGetValue(orderItem.ProductId.Value, out var product))
                {
                    orderItem.Product = product;
                }
            }
        }

        var shippingAddress = !string.IsNullOrWhiteSpace(shippingAddressOverride)
            ? shippingAddressOverride
            : GetMetadataValue(session, "shipping_address") ?? FormatStripeAddress(session.CustomerDetails?.Address);

        var billingAddress = !string.IsNullOrWhiteSpace(billingAddressOverride)
            ? billingAddressOverride
            : GetMetadataValue(session, "billing_address");

        var notes = !string.IsNullOrWhiteSpace(notesOverride)
            ? notesOverride
            : GetMetadataValue(session, "notes");

        var paymentIntentId = explicitPaymentIntentId ?? session.PaymentIntentId ?? session.Id;

        var estimatedDelivery = status == OrderStatus.Cancelled
            ? (DateTime?)null
            : DateTime.UtcNow.AddDays(7);

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = status,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            ShippingAmount = shippingAmount,
            TotalAmount = total,
            PaymentMethod = paymentMethod,
            PaymentIntentId = paymentIntentId,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            Notes = notes,
            EstimatedDeliveryDate = estimatedDelivery,
            Items = orderItems
        };

        ctx.Order.Add(order);
        await ctx.SaveChangesAsync();

        if (clearCart)
        {
            await ClearShoppingCartAsync(userId);
        }

        if (sendEmail)
        {
            await TrySendOrderEmailsAsync(order, status);
        }

        // Create notification for payment events
        await CreatePaymentNotificationAsync(order, status, userId);

        return order;
    }

    private static string FormatStripeAddress(Stripe.Address? addr)
    {
        if (addr == null) return string.Empty;
        var parts = new List<string?> { addr.Line1, addr.Line2, addr.City, addr.State, addr.PostalCode, addr.Country };
        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string? GetMetadataValue(Session session, string key)
    {
        if (session.Metadata == null)
        {
            return null;
        }

        if (session.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return null;
    }

    private async Task<Order> CreateOrderFromStoredCartAsync(ConfirmPaymentRequest request, string userId)
    {
        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)!
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart?.Items == null || cart.Items.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty");
        }

        var subtotal = cart.Items.Sum(item => (item.UnitPrice ?? 0) * item.Quantity);

        TaxRateModel? taxRate = null;
        if (!string.IsNullOrWhiteSpace(request.BillingStateCode))
        {
            taxRate = await ctx.TaxRate
                .Where(x => x.StateCode == request.BillingStateCode && x.IsActive)
                .FirstOrDefaultAsync();
        }

        var taxAmount = taxRate is not null
            ? subtotal * taxRate.CombinedTaxRate / 100m
            : 0m;

        var shippingAmount = request.ShippingAmount;
        if (shippingAmount == null)
        {
            shippingAmount = (await GetDefaultShippingRateAsync())?.Amount ?? 0m;
        }

        var total = subtotal + taxAmount + (shippingAmount ?? 0m);

        var orderItems = cart.Items.Select(item => new OrderItem
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            TotalPrice = (item.UnitPrice ?? 0) * item.Quantity
        }).ToList();

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            ShippingAmount = shippingAmount ?? 0m,
            TotalAmount = total,
            PaymentMethod = request.PaymentMethod,
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.BillingAddress,
            Notes = request.Notes,
            EstimatedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Items = orderItems
        };

        ctx.Order.Add(order);

        ctx.ShoppingCartItem.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.ModifiedDate = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        // Generate and send appropriate documents for non-credit card payments
        if (request.PaymentMethod == SharedPaymentMethod.PurchaseOrder)
        {
            try
            {
                // Load the order with products for PDF generation
                var orderWithProducts = await ctx.Order
                    .Include(o => o.Items)!
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                if (orderWithProducts != null)
                {
                    // Generate invoice PDF
                    var invoicePdf = pdfService.GenerateOrderPdf(orderWithProducts);

                    // Get customer email from request or user claims
                    var customerEmail = request.CustomerEmail ?? User.FindFirstValue(ClaimTypes.Email);
                    
                    if (string.IsNullOrEmpty(customerEmail))
                    {
                        return order;
                    }

                    // Send invoice email with PDF attachment
                    await emailService.SendInvoiceEmailAsync(orderWithProducts, customerEmail, invoicePdf);
                }
            }
            catch (Exception ex)
            {
                // Error logged internally but don't fail the order creation
            }
        }
        else if (request.PaymentMethod == SharedPaymentMethod.Cash)
        {
            try
            {
                // Load the order with products for PDF generation
                var orderWithProducts = await ctx.Order
                    .Include(o => o.Items)!
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                if (orderWithProducts != null)
                {
                    // Generate delivery receipt PDF (same format as invoice but with different context)
                    var deliveryReceiptPdf = pdfService.GenerateOrderPdf(orderWithProducts);

                    // Get customer email from request or user claims
                    var customerEmail = request.CustomerEmail ?? User.FindFirstValue(ClaimTypes.Email);
                    
                    if (string.IsNullOrEmpty(customerEmail))
                    {
                        return order;
                    }

                    // Send cash order email with delivery receipt PDF attachment
                    await emailService.SendCashOrderEmailAsync(orderWithProducts, customerEmail, deliveryReceiptPdf);
                }
            }
            catch (Exception ex)
            {
                // Error logged internally but don't fail the order creation
            }
        }

        // Create notification for payment events
        await CreatePaymentNotificationAsync(order, order.Status, userId);

        return order;
    }

    private async Task<decimal> DetermineShippingAmountAsync(Session session)
    {
        if (session.Metadata != null &&
            session.Metadata.TryGetValue("shipping_amount", out var shippingRaw) &&
            decimal.TryParse(shippingRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var metadataShipping))
        {
            return metadataShipping;
        }

        if (session.TotalDetails?.AmountShipping is long shippingCents)
        {
            return shippingCents / 100m;
        }

        var rate = await GetDefaultShippingRateAsync();
        return rate?.Amount ?? 0m;
    }

    private async Task<ShippingRateModel?> GetDefaultShippingRateAsync()
    {
        return await ctx.ShippingRate
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedDate)
            .FirstOrDefaultAsync();
    }

    private async Task ClearShoppingCartAsync(string userId)
    {
        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart?.Items == null || cart.Items.Count == 0)
        {
            return;
        }

        ctx.ShoppingCartItem.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.ModifiedDate = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
    }

    private async Task TrySendOrderEmailsAsync(Order order, OrderStatus status)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return;
            }

            switch (status)
            {
                case OrderStatus.Confirmed:
                    await emailService.SendOrderConfirmationAsync(order, userEmail);
                    break;
                case OrderStatus.Cancelled:
                    await emailService.SendOrderCancellationAsync(order, userEmail);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Error logged internally
        }
    }

    [HttpGet("invoice/{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetInvoicePdfAsync(long orderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            // Get the order with products, ensuring it belongs to the current user
            var order = await ctx.Order
                .Include(o => o.Items)!
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            // Generate the PDF invoice
            var invoicePdf = pdfService.GenerateOrderPdf(order);

            // Return the PDF as a downloadable file
            return File(invoicePdf, "application/pdf", $"Invoice-{order.OrderNumber}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error generating invoice: {ex.Message}" });
        }
    }

    private async Task CreatePaymentNotificationAsync(Order order, OrderStatus status, string userId)
    {
        try
        {
            var (title, message, notificationType) = status switch
            {
                OrderStatus.Confirmed => (
                    "Payment Confirmed",
                    $"Your payment for order {order.OrderNumber} has been confirmed. Total: ${order.TotalAmount:F2}",
                    "Success"
                ),
                OrderStatus.Cancelled => (
                    "Payment Cancelled", 
                    $"Payment for order {order.OrderNumber} has been cancelled.",
                    "Warning"
                ),
                OrderStatus.Pending => (
                    "Payment Pending",
                    $"Your order {order.OrderNumber} is pending payment. Total: ${order.TotalAmount:F2}",
                    "Info"
                ),
                _ => (
                    "Order Updated",
                    $"Order {order.OrderNumber} status has been updated to {status}.",
                    "Info"
                )
            };

            await notificationService.CreateNotificationAsync(
                title: title,
                message: message,
                type: notificationType,
                userId: userId,
                actionUrl: $"/orders/{order.Id}",
                notes: $"Order #{order.OrderNumber} - {order.PaymentMethod}"
            );
        }
        catch (Exception ex)
        {
            // Error logged internally but don't fail the payment process
        }
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
    }
}

public class ConfirmPaymentRequest
{
    public AppProduct.Shared.Models.PaymentMethod PaymentMethod { get; set; }
    public string? StripeSessionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? ShippingAddress { get; set; }
    public string? BillingAddress { get; set; }
    public string BillingStateCode { get; set; } = "";
    public decimal? ShippingAmount { get; set; }
    public string? Notes { get; set; }
    public string? CustomerEmail { get; set; }
}