using System.Security.Claims;
using System.Text.Json;
using AppProduct.Data;
using AppProduct.Services;
using AppProduct.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;

namespace AppProduct.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ODataController
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(ApplicationDbContext context, INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    [EnableQuery]
    public IQueryable<Notification> Get()
    {
        var userId = GetCurrentUserId();
        return _context.Notification
            .Where(n => n.UserId == userId || n.UserId == null)
            .OrderByDescending(n => n.CreatedDate);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Notification>> Get(long id)
    {
        var notification = await _context.Notification.FindAsync(id);
        if (notification == null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        if (notification.UserId != null && notification.UserId != userId)
        {
            return Forbid();
        }

        return notification;
    }

    [HttpPost]
    public async Task<ActionResult<Notification>> Post([FromBody] Notification notification)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        notification.CreatedDate = DateTime.UtcNow;
        notification.IsRead = false;

        _context.Notification.Add(notification);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = notification.Id }, notification);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Notification>> Put(long id, [FromBody] Notification notification)
    {
        if (id != notification.Id)
        {
            return BadRequest();
        }

        var existingNotification = await _context.Notification.FindAsync(id);
        if (existingNotification == null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        if (existingNotification.UserId != null && existingNotification.UserId != userId)
        {
            return Forbid();
        }

        existingNotification.Title = notification.Title;
        existingNotification.Message = notification.Message;
        existingNotification.Type = notification.Type;
        existingNotification.ActionUrl = notification.ActionUrl;
        existingNotification.Notes = notification.Notes;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!NotificationExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return existingNotification;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var notification = await _context.Notification.FindAsync(id);
        if (notification == null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        if (notification.UserId != null && notification.UserId != userId)
        {
            return Forbid();
        }

        _context.Notification.Remove(notification);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("markAsRead/{id}")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok();
    }

    [HttpPost("markAllAsRead")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok();
    }

    [HttpGet("unreadCount")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(count);
    }

    [HttpGet("stream")]
    public async Task StreamNotifications(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("Access-Control-Allow-Origin", "*");

        _logger.LogInformation("Starting SSE stream for user {UserId}", userId);

        try
        {
            await foreach (var notification in _notificationService.GetNotificationStream(userId, cancellationToken))
            {
                var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE stream cancelled for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE stream for user {UserId}", userId);
        }
    }

    [HttpPost("bulkUpsert")]
    public async Task<IActionResult> BulkUpsert([FromBody] List<Notification> notifications)
    {
        var processedCount = 0;
        var addedCount = 0;
        var updatedCount = 0;

        foreach (var notification in notifications)
        {
            try
            {
                if (notification.Id.HasValue && notification.Id > 0)
                {
                    var existing = await _context.Notification.FindAsync(notification.Id.Value);
                    if (existing != null)
                    {
                        existing.Title = notification.Title ?? existing.Title;
                        existing.Message = notification.Message ?? existing.Message;
                        existing.Type = notification.Type ?? existing.Type;
                        existing.ActionUrl = notification.ActionUrl ?? existing.ActionUrl;
                        existing.Notes = notification.Notes ?? existing.Notes;
                        updatedCount++;
                    }
                    else
                    {
                        notification.CreatedDate = DateTime.UtcNow;
                        notification.IsRead = false;
                        _context.Notification.Add(notification);
                        addedCount++;
                    }
                }
                else
                {
                    notification.CreatedDate = DateTime.UtcNow;
                    notification.IsRead = false;
                    _context.Notification.Add(notification);
                    addedCount++;
                }
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification during bulk upsert");
            }
        }

        try
        {
            await _context.SaveChangesAsync();
            return Ok(new { success = true, processedCount, addedCount, updatedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving bulk upsert notifications");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // Convert string user ID to long - you may need to adjust this based on your user ID format
        if (long.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        // If using string IDs, you might need to hash or map to a long value
        // For now, using a simple hash
        return Math.Abs(userIdClaim.GetHashCode());
    }

    private bool NotificationExists(long id)
    {
        return _context.Notification.Any(e => e.Id == id);
    }
}