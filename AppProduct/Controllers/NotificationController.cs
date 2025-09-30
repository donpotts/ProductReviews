using System.Security.Claims;
using AppProduct.Data;
using AppProduct.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AppProduct.Controllers;

// OData Controller for /odata/Notification endpoint
[Authorize]
public class NotificationController : ODataController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(ApplicationDbContext context, ILogger<NotificationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [EnableQuery]
    public IQueryable<Notification> Get()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Log all notifications in database for debugging
            var allNotifications = _context.Notification.ToList();
            _logger.LogInformation($"=== DEBUG: Total notifications in database: {allNotifications.Count}");
            foreach (var notif in allNotifications)
            {
                _logger.LogInformation($"Notification ID={notif.Id}, UserId={notif.UserId}, Title={notif.Title}");
            }
            _logger.LogInformation($"=== Current user ID: {userId}");
            
            var query = _context.Notification
                .Where(n => n.UserId == userId || n.UserId == null)
                .OrderByDescending(n => n.CreatedDate);
            
            // Log debugging info
            var totalCount = query.Count();
            var readCount = query.Count(n => n.IsRead);
            var unreadCount = query.Count(n => !n.IsRead);
            _logger.LogInformation($"User {userId}: Total={totalCount}, Read={readCount}, Unread={unreadCount}");
                
            return query;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user");
            throw;
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
}