using AppProduct.Shared.Models;

namespace AppProduct.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(string title, string message, string type = "Info", string? userId = null, string? actionUrl = null, string? notes = null);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(long notificationId);
    Task MarkAllAsReadAsync(string userId);
    IAsyncEnumerable<Notification> GetNotificationStream(string userId, CancellationToken cancellationToken);
}