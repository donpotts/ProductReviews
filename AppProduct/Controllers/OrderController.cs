using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AppProduct.Data;
using AppProduct.Shared.Models;
using AppProduct.Services;
using System.Security.Claims;

namespace AppProduct.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("Fixed")]
public class OrderController(ApplicationDbContext ctx, IEmailNotificationService emailService) : ControllerBase
{
    [HttpGet("")]
    [EnableQuery]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IQueryable<Order>> Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var isAdmin = User.IsInRole("Administrator");
        var orders = isAdmin 
            ? ctx.Order.Include(x => x.Items).ThenInclude(x => x.Product)
            : ctx.Order.Where(x => x.UserId == userId).Include(x => x.Items).ThenInclude(x => x.Product);

        return Ok(orders);
    }

    [HttpGet("{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Order>> GetAsync(long key)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var isAdmin = User.IsInRole("Administrator");
        var order = await ctx.Order
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == key && (isAdmin || x.UserId == userId));

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost("create-from-cart")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Order>> CreateFromCartAsync([FromBody] CreateOrderRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var cart = await ctx.ShoppingCart
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (cart == null || cart.Items == null || !cart.Items.Any())
            return BadRequest("Cart is empty");

        // Calculate totals
        var subtotal = cart.Items.Sum(item => (item.UnitPrice ?? 0) * item.Quantity);
        
        // Calculate tax
        var taxRate = await ctx.TaxRate.FirstOrDefaultAsync(x => x.StateCode == request.BillingStateCode && x.IsActive);
        var taxAmount = taxRate != null ? subtotal * taxRate.CombinedTaxRate / 100 : 0;
        
        var total = subtotal + taxAmount + (request.ShippingAmount ?? 0);

        // Create order
        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            ShippingAmount = request.ShippingAmount ?? 0,
            TotalAmount = total,
            PaymentMethod = request.PaymentMethod,
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.BillingAddress,
            Notes = request.Notes,
            EstimatedDeliveryDate = DateTime.UtcNow.AddDays(7), // Default 7 days
            Items = cart.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = (item.UnitPrice ?? 0) * item.Quantity
            }).ToList()
        };

        ctx.Order.Add(order);

        // Clear the cart
        ctx.ShoppingCartItem.RemoveRange(cart.Items);
        cart.Items.Clear();

        await ctx.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAsync), new { key = order.Id }, order);
    }

    [HttpPut("{key}/status")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Order>> UpdateStatusAsync(long key, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await ctx.Order.FindAsync(key);
        if (order == null)
            return NotFound();

        order.Status = request.Status;
        
        if (request.TrackingNumber != null)
            order.TrackingNumber = request.TrackingNumber;

        if (request.Status == OrderStatus.Shipped && order.ActualDeliveryDate == null)
            order.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(3);

        if (request.Status == OrderStatus.Delivered)
            order.ActualDeliveryDate = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        return Ok(order);
    }

    [HttpPost("{key}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Order>> CancelOrderAsync(long key)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var isAdmin = User.IsInRole("Administrator");
        var order = await ctx.Order.FirstOrDefaultAsync(x => x.Id == key && (isAdmin || x.UserId == userId));

        if (order == null)
            return NotFound();

        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            return BadRequest("Cannot cancel shipped or delivered orders");

        order.Status = OrderStatus.Cancelled;
        await ctx.SaveChangesAsync();

        return Ok(order);
    }

    [HttpPost("send-confirmation-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SendConfirmationEmailAsync([FromBody] SendOrderEmailRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var isAdmin = User.IsInRole("Administrator");
        var order = await ctx.Order
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId && (isAdmin || x.UserId == userId));

        if (order == null)
            return NotFound();

        try
        {
            await emailService.SendOrderConfirmationAsync(order, request.CustomerEmail);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to send email: {ex.Message}");
        }
    }

    private static string GenerateOrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}";
    }
}

public class CreateOrderRequest
{
    public string? ShippingAddress { get; set; }
    public string? BillingAddress { get; set; }
    public string BillingStateCode { get; set; } = "";
    public decimal? ShippingAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
    public string? TrackingNumber { get; set; }
}

public class SendOrderEmailRequest
{
    public long OrderId { get; set; }
    public string CustomerEmail { get; set; } = "";
}