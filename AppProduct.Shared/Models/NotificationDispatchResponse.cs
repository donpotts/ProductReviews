using System.Collections.Generic;

namespace AppProduct.Shared.Models;

public class NotificationDispatchResponse
{
    public int CreatedCount { get; set; }
    public IReadOnlyList<Notification> Notifications { get; set; } = new List<Notification>();
}
