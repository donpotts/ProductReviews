using System;
using System.Collections.Generic;

namespace AppProduct.Shared.Models;

public class NotificationDispatchRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public IEnumerable<string>? TargetUserIds { get; set; }
        = Array.Empty<string>();
    public bool BroadcastToAll { get; set; }
        = false;
    public bool IncludeSender { get; set; }
        = false;
    public string? ActionUrl { get; set; }
        = null;
    public string? Notes { get; set; }
        = null;
}
