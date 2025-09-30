using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using AppProduct.Data;
using AppProduct.Services;
using AppProduct.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace AppProduct.Controllers;

[Route("api/notification")]
[ApiController]
[Authorize]
public class NotificationApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationApiController> _logger;

    public NotificationApiController(ApplicationDbContext context, INotificationService notificationService, ILogger<NotificationApiController> logger)
    {
        _context = context;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    [EnableQuery]
    public IQueryable<Notification> Get()
    {
        try
        {
            _logger.LogInformation("=== NOTIFICATION GET REQUEST START ===");
            var userId = GetCurrentUserId();
            _logger.LogInformation($"User ID: {userId}");
            
            var query = _context.Notification
                .Where(n => n.UserId == userId || n.UserId == null)
                .OrderByDescending(n => n.CreatedDate);
                
            var count = query.Count();
            _logger.LogInformation($"Found {count} notifications for user {userId}");
            _logger.LogInformation("=== NOTIFICATION GET REQUEST SUCCESS ===");
            
            return query;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== NOTIFICATION GET REQUEST ERROR ===");
            throw;
        }
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

        if (string.IsNullOrWhiteSpace(notification.Title) || string.IsNullOrWhiteSpace(notification.Message))
        {
            return BadRequest("Title and message are required");
        }

        var created = await _notificationService.CreateNotificationAsync(
            notification.Title,
            notification.Message,
            string.IsNullOrWhiteSpace(notification.Type) ? "Info" : notification.Type!,
            notification.UserId,
            notification.ActionUrl,
            notification.Notes);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("send")]
    public async Task<ActionResult<NotificationDispatchResponse>> SendAsync([FromBody] NotificationDispatchRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Title and message are required");
        }

    var resolvedType = string.IsNullOrWhiteSpace(request.Type) ? "Info" : request.Type!;
        var recipients = new HashSet<string>();

        if (request.TargetUserIds != null)
        {
            foreach (var targetId in request.TargetUserIds)
            {
                if (!string.IsNullOrEmpty(targetId))
                {
                    recipients.Add(targetId);
                }
            }
        }

        if (request.IncludeSender)
        {
            recipients.Add(GetCurrentUserId());
        }

        if (!request.BroadcastToAll && recipients.Count == 0)
        {
            return BadRequest("Specify at least one target user or enable broadcast");
        }

        var createdNotifications = new List<Notification>();

        if (request.BroadcastToAll)
        {
            createdNotifications.Add(await _notificationService.CreateNotificationAsync(
                request.Title,
                request.Message,
                resolvedType,
                null,
                request.ActionUrl,
                request.Notes));
        }

        foreach (var recipientId in recipients)
        {
            createdNotifications.Add(await _notificationService.CreateNotificationAsync(
                request.Title,
                request.Message,
                resolvedType,
                recipientId,
                request.ActionUrl,
                request.Notes));
        }

        var response = new NotificationDispatchResponse
        {
            CreatedCount = createdNotifications.Count,
            Notifications = createdNotifications
        };

        return Ok(response);
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

    [HttpPost("fix-user-ids")]
    public async Task<ActionResult> FixNotificationUserIds()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            // Get all notifications that don't match the current user ID
            var notifications = await _context.Notification
                .Where(n => n.UserId != currentUserId && n.UserId != null)
                .ToListAsync();
            
            _logger.LogInformation($"Found {notifications.Count} notifications with mismatched user IDs");
            _logger.LogInformation($"Current user ID: {currentUserId}");
            
            foreach (var notification in notifications)
            {
                _logger.LogInformation($"Updating notification {notification.Id} from UserId {notification.UserId} to {currentUserId}");
                notification.UserId = currentUserId;
            }
            
            await _context.SaveChangesAsync();
            
            return Ok(new { message = $"Updated {notifications.Count} notifications to user ID {currentUserId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing notification user IDs");
            return StatusCode(500, ex.Message);
        }
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

    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        return userIdClaim;
    }

    private bool NotificationExists(long id)
    {
        return _context.Notification.Any(e => e.Id == id);
    }
}