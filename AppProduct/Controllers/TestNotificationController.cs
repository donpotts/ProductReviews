using AppProduct.Data;
using AppProduct.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Security.Claims;

namespace AppProduct.Controllers;

/*
// Test controller commented out for production
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TestNotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TestNotificationController> _logger;

    public TestNotificationController(ApplicationDbContext context, ILogger<TestNotificationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [EnableQuery]
    public ActionResult<IQueryable<Notification>> Get()
    {
        try
        {
            _logger.LogInformation("=== TEST NOTIFICATION GET START ===");
            var userId = GetCurrentUserId();
            _logger.LogInformation($"User ID: {userId}");
            
            var query = _context.Notification
                .Where(n => n.UserId == userId || n.UserId == null)
                .OrderByDescending(n => n.CreatedDate);
                
            var count = query.Count();
            _logger.LogInformation($"Found {count} notifications for user {userId}");
            
            return Ok(query);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== TEST NOTIFICATION GET ERROR ===");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("simple")]
    public ActionResult<IEnumerable<Notification>> GetSimple()
    {
        try
        {
            _logger.LogInformation("=== TEST NOTIFICATION SIMPLE GET START ===");
            var userId = GetCurrentUserId();
            _logger.LogInformation($"User ID: {userId}");
            
            var notifications = _context.Notification
                .Where(n => n.UserId == userId || n.UserId == null)
                .OrderByDescending(n => n.CreatedDate)
                .Take(10)
                .ToList();
                
            _logger.LogInformation($"Found {notifications.Count} notifications for user {userId}");
            
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== TEST NOTIFICATION SIMPLE GET ERROR ===");
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("create-test-notifications")]
    public async Task<ActionResult> CreateTestNotifications()
    {
        try
        {
            var userId = GetCurrentUserId();
            var notifications = new[]
            {
                new { Title = "Payment Confirmed", Message = "Your payment for order ORD-20250929-123456 has been confirmed. Total: $99.99", Type = "Success" },
                new { Title = "Order Shipped", Message = "Good news! Your order ORD-20250929-123457 has been shipped. Tracking: TRACK123", Type = "Success" },
                new { Title = "Payment Cancelled", Message = "Payment for order ORD-20250929-123458 has been cancelled.", Type = "Warning" },
                new { Title = "Order Delivered", Message = "Your order ORD-20250929-123459 has been delivered successfully!", Type = "Success" },
                new { Title = "Payment Pending", Message = "Your order ORD-20250929-123460 is pending payment. Total: $149.99", Type = "Info" }
            };

            var createdCount = 0;
            foreach (var notif in notifications)
            {
                await _context.Notification.AddAsync(new Notification
                {
                    Title = notif.Title,
                    Message = notif.Message,
                    Type = notif.Type,
                    UserId = userId,
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow,
                    ActionUrl = "/orders"
                });
                createdCount++;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Created {createdCount} test notifications" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        if (long.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return Math.Abs(userIdClaim.GetHashCode());
    }
}
*/