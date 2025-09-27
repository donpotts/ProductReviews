using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;
using AppProduct.Services;
using System.Security.Claims;

namespace AppProduct.Controllers;

public class CheckoutCancellationRequest
{
    public List<CartProduct>? Cart { get; set; }
    public string? SessionId { get; set; }
}

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class EmailController(ApplicationDbContext ctx, IEmailNotificationService emailService, IConfiguration configuration) : ControllerBase
{
    [HttpPost("order-confirmation/{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> SendOrderConfirmationAsync(long orderId, [FromBody] dynamic request)
    {
        try
        {
            var order = await ctx.Order
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound("Order not found");

            string customerEmail = request.CustomerEmail;
            if (string.IsNullOrEmpty(customerEmail))
                return BadRequest("Customer email is required");

            await emailService.SendOrderConfirmationAsync(order, customerEmail);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to send email: {ex.Message}");
        }
    }

    [HttpPost("checkout-cancellation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> SendCheckoutCancellationAsync([FromBody] CheckoutCancellationRequest request)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
                return BadRequest("User email not found");

            List<CartProduct> cart = request.Cart ?? new List<CartProduct>();
            string? sessionId = request.SessionId;

            Console.WriteLine($"Checkout cancellation - Cart items: {cart?.Count ?? 0}, SessionId: {sessionId}");

            // If we have a session ID but no cart items, try to retrieve from Stripe
            if ((cart == null || !cart.Any()) && !string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("Attempting to retrieve cart from Stripe session...");
                var stripeCart = await RetrieveCartFromStripeSession(sessionId);
                if (stripeCart.Any())
                {
                    cart = stripeCart;
                    Console.WriteLine($"Retrieved {cart.Count} items from Stripe session");
                }
                else
                {
                    Console.WriteLine("No items retrieved from Stripe session");
                }
            }

            if (cart?.Any() == true)
            {
                Console.WriteLine($"Sending cancellation email with {cart.Count} items to {userEmail}");
            }
            else
            {
                Console.WriteLine($"Sending cancellation email with no items to {userEmail}");
            }

            await emailService.SendCheckoutCancellationEmailAsync(cart ?? new List<CartProduct>(), userEmail);
            return Ok();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in checkout cancellation: {ex.Message}");
            Console.WriteLine($"Exception details: {ex}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return BadRequest($"Failed to send cancellation email: {ex.Message}");
        }
    }

    private async Task<List<CartProduct>> RetrieveCartFromStripeSession(string sessionId)
    {
        try
        {
            var stripeSecretKey = configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeSecretKey))
            {
                Console.WriteLine("Stripe secret key not configured");
                return new List<CartProduct>();
            }

            Console.WriteLine($"Retrieving Stripe session: {sessionId}");
            Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
            var service = new Stripe.Checkout.SessionService();
            var session = await service.GetAsync(sessionId, new Stripe.Checkout.SessionGetOptions
            {
                Expand = new List<string> { "line_items", "line_items.data.price.product" }
            });

            Console.WriteLine($"Stripe session status: {session.Status}, Line items count: {session.LineItems?.Data?.Count ?? 0}");

            if (session.LineItems?.Data != null && session.LineItems.Data.Any())
            {
                var cartItems = session.LineItems.Data
                    .Where(item => item.Quantity > 0 && item.AmountTotal > 0)
                    .Select(item => {
                        var unitPrice = item.Quantity > 0 ? (decimal)item.AmountTotal / (decimal)item.Quantity / 100 : 0;
                        var productName = item.Price?.Product?.Name ?? item.Description ?? "Product";
                        
                        Console.WriteLine($"Processing line item: {productName}, Amount: {item.AmountTotal}, Qty: {item.Quantity}, Unit Price: ${unitPrice}");
                        
                        return new CartProduct
                        {
                            Id = null, // We don't have the original product ID
                            Name = productName,
                            Description = item.Price?.Product?.Description ?? item.Description ?? "",
                            Price = unitPrice,
                            Quantity = (int)item.Quantity
                        };
                    }).ToList();

                Console.WriteLine($"Converted {cartItems.Count} line items to cart products");
                return cartItems;
            }
            else
            {
                Console.WriteLine("No line items found in Stripe session");
            }
        }
        catch (Stripe.StripeException stripeEx)
        {
            Console.WriteLine($"Stripe error retrieving session {sessionId}: {stripeEx.Message}");
            Console.WriteLine($"Stripe error code: {stripeEx.StripeError?.Code}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to retrieve cart from Stripe session: {ex.Message}");
            Console.WriteLine($"Exception details: {ex}");
        }

        return new List<CartProduct>();
    }

    [HttpPost("order-cancellation/{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> SendOrderCancellationAsync(long orderId)
    {
        try
        {
            var order = await ctx.Order
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound("Order not found");

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
                return BadRequest("User email not found");

            await emailService.SendOrderCancellationAsync(order, userEmail);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to send order cancellation email: {ex.Message}");
        }
    }
}