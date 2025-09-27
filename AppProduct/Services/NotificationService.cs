using System.Collections.Concurrent;
using System.Text.Json;
using System.Runtime.CompilerServices;
using AppProduct.Data;
using AppProduct.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AppProduct.Services;

public class NotificationService : INotificationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationService> _logger;
    
    // Store subscribers for each user
    private readonly ConcurrentDictionary<long, List<TaskCompletionSource<Notification>>> _subscribers = new();

    public NotificationService(IServiceProvider serviceProvider, ILogger<NotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Notification> CreateNotificationAsync(
        string title,
        string message,
        string type = "Info",
        long? userId = null,
        string? actionUrl = null,
        string? notes = null)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = string.IsNullOrWhiteSpace(type) ? "Info" : type,
            UserId = userId,
            ActionUrl = actionUrl,
            Notes = notes,
            CreatedDate = DateTime.UtcNow,
            IsRead = false
        };

        // Save to database
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        context.Notification.Add(notification);
        await context.SaveChangesAsync();

        _logger.LogInformation("Created notification: {Title} for user: {UserId}", title, userId);

        // Notify SSE subscribers
        if (userId.HasValue && _subscribers.TryGetValue(userId.Value, out var userSubscribers))
        {
            var subscribersToNotify = userSubscribers.ToList();
            userSubscribers.Clear();

            foreach (var subscriber in subscribersToNotify)
            {
                try
                {
                    subscriber.SetResult(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify SSE subscriber for user {UserId}", userId);
                }
            }
        }

        return notification;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(long userId, bool unreadOnly = false)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var query = context.Notification
            .Where(n => n.UserId == userId || n.UserId == null) // Include system-wide notifications
            .OrderByDescending(n => n.CreatedDate);

        if (unreadOnly)
        {
            query = (IOrderedQueryable<Notification>)query.Where(n => !n.IsRead);
        }

        return await query.Take(50).ToListAsync(); // Limit to 50 most recent
    }

    public async Task<int> GetUnreadCountAsync(long userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Notification
            .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(long notificationId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var notification = await context.Notification.FindAsync(notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadDate = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(long userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var unreadNotifications = await context.Notification
            .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadDate = DateTime.UtcNow;
        }

        if (unreadNotifications.Any())
        {
            await context.SaveChangesAsync();
        }
    }

    public async IAsyncEnumerable<Notification> GetNotificationStream(long userId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SSE stream for user {UserId}", userId);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<Notification>();
                
                // Add this subscriber to the user's list
                _subscribers.AddOrUpdate(userId, 
                    new List<TaskCompletionSource<Notification>> { tcs },
                    (key, existing) => { existing.Add(tcs); return existing; });

                // Register cancellation to clean up if client disconnects
                using var registration = cancellationToken.Register(() => 
                {
                    if (_subscribers.TryGetValue(userId, out var subs))
                    {
                        subs.Remove(tcs);
                        if (!subs.Any())
                        {
                            _subscribers.TryRemove(userId, out _);
                        }
                    }
                    tcs.TrySetCanceled();
                });

                Notification notification;
                try
                {
                    // Wait for a notification to be created for this user
                    notification = await tcs.Task;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("SSE stream cancelled for user {UserId}", userId);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SSE stream for user {UserId}", userId);
                    break;
                }
                
                yield return notification;
            }
        }
        finally
        {
            // Clean up subscriber
            if (_subscribers.TryGetValue(userId, out var userSubs))
            {
                userSubs.Clear();
                if (!userSubs.Any())
                {
                    _subscribers.TryRemove(userId, out _);
                }
            }
            
            _logger.LogInformation("SSE stream ended for user {UserId}", userId);
        }
    }
}