using AppProduct.Shared.Models;

namespace AppProduct.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(string title, string message, string type = "Info", long? userId = null, string? actionUrl = null, string? notes = null);
    Task<List<Notification>> GetUserNotificationsAsync(long userId, bool unreadOnly = false);
    Task<int> GetUnreadCountAsync(long userId);
    Task MarkAsReadAsync(long notificationId);
    Task MarkAllAsReadAsync(long userId);
    IAsyncEnumerable<Notification> GetNotificationStream(long userId, CancellationToken cancellationToken);
}