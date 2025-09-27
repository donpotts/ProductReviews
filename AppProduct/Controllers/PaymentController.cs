using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;
using AppProduct.Services;
using System.Security.Claims;
using Stripe;
using Stripe.Checkout;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class PaymentController(ApplicationDbContext ctx, IConfiguration configuration, IEmailNotificationService emailService) : ControllerBase
{
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

        Console.WriteLine("START CART LOG");
        Console.WriteLine("================================================");
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(cart, jsonOptions));
        Console.WriteLine("END CART LOG");
        Console.WriteLine("================================================");

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

            var domain = $"{Request.Scheme}://{Request.Host}";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = cart.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)((item.Price ?? 0) * 100), // Convert to cents
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Name ?? "Product",
                            Description = item.Description ?? "",
                        },
                    },
                    Quantity = item.Quantity,
                }).ToList(),
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
            Console.WriteLine($"Stripe session creation error: {ex}");
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
            if (request.PaymentMethod == AppProduct.Shared.Models.PaymentMethod.CreditCard)
            {
                if (string.IsNullOrEmpty(request.StripeSessionId))
                    return BadRequest(new { error = "Stripe session ID is required for credit card payments" });

                StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
                var service = new SessionService();
                var session = await service.GetAsync(request.StripeSessionId);

                if (session.PaymentStatus != "paid")
                    return BadRequest(new { error = "Payment not completed" });

                request.PaymentIntentId = session.PaymentIntentId;
            }

            var order = await CreateOrderFromCartAsync(request, request.PaymentIntentId);
            return Ok(order);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Error confirming payment: {ex.Message}" });
        }
    }

    private async Task<Order> CreateOrderFromCartAsync(ConfirmPaymentRequest request, string? paymentIntentId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            throw new UnauthorizedAccessException();

        // Since we're using CartService with local storage, we need to get the Stripe session 
        // to retrieve the cart items that were used in the payment
        if (request.PaymentMethod == AppProduct.Shared.Models.PaymentMethod.CreditCard && !string.IsNullOrEmpty(request.StripeSessionId))
        {
            StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
            var service = new SessionService();
            var session = await service.GetAsync(request.StripeSessionId, new SessionGetOptions
            {
                Expand = new List<string> { "line_items" }
            });

            if (session.LineItems == null || !session.LineItems.Data.Any())
                throw new InvalidOperationException("No items found in payment session");

            // Calculate totals from Stripe session
            var subtotal = session.LineItems.Data
                .Where(item => !item.Description?.Contains("Tax") == true && !item.Description?.Contains("Shipping") == true)
                .Sum(item => (decimal)item.AmountTotal / 100); // Stripe amounts are in cents

            var taxAmount = session.LineItems.Data
                .Where(item => item.Description?.Contains("Tax") == true)
                .Sum(item => (decimal)item.AmountTotal / 100);

            var shippingAmount = session.LineItems.Data
                .Where(item => item.Description?.Contains("Shipping") == true)
                .Sum(item => (decimal)item.AmountTotal / 100);

            var total = (decimal)session.AmountTotal / 100;

            var order = new Order
            {
                OrderNumber = GenerateOrderNumber(),
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Confirmed,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                ShippingAmount = shippingAmount,
                TotalAmount = total,
                PaymentMethod = request.PaymentMethod,
                PaymentIntentId = paymentIntentId,
                ShippingAddress = request.ShippingAddress ?? session.ShippingDetails?.Address?.ToString(),
                BillingAddress = request.BillingAddress,
                Notes = request.Notes,
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(7),
                Items = session.LineItems.Data
                    .Where(item => !item.Description?.Contains("Tax") == true && !item.Description?.Contains("Shipping") == true)
                    .Select(item => new OrderItem
                    {
                        ProductId = null, // We'll need to resolve this differently
                        Quantity = (int)item.Quantity,
                        UnitPrice = (decimal)item.AmountTotal / (decimal)item.Quantity / 100,
                        TotalPrice = (decimal)item.AmountTotal / 100
                    }).ToList()
            };

            ctx.Order.Add(order);
            await ctx.SaveChangesAsync();

            // Send email notifications for successful order
            try
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(userEmail))
                {
                    await emailService.SendOrderConfirmationAsync(order, userEmail);
                }
            }
            catch (Exception ex)
            {
                // Log email error but don't fail the order creation
                Console.WriteLine($"Failed to send order confirmation email: {ex.Message}");
            }

            return order;
        }

        throw new InvalidOperationException("Unable to create order from cart");
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
}