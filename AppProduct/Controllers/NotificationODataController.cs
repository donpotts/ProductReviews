using System.Linq;
using System.Security.Claims;
using AppProduct.Data;
using AppProduct.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppProduct.Controllers;

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

    [EnableQuery(PageSize = 100, HandleNullPropagation = HandleNullPropagationOption.True)]
    public IActionResult Get()
    {
        try
        {
            var userId = GetCurrentUserId();
            var query = _context.Notification
                .Where(n => n.UserId == userId || n.UserId == null)
                .OrderByDescending(n => n.CreatedDate);

            return Ok(query);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications via OData");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve notifications");
        }
    }

    [EnableQuery]
    public SingleResult<Notification> Get([FromODataUri] long key)
    {
        try
        {
            var userId = GetCurrentUserId();

            var result = _context.Notification
                .Where(n => n.Id == key && (n.UserId == userId || n.UserId == null));

            return SingleResult.Create(result);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification {NotificationId} via OData", key);
            throw;
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
