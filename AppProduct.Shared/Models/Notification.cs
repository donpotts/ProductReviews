namespace AppProduct.Shared.Models;

public class Notification
{
    public long? Id { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? Type { get; set; } // "Info", "Warning", "Error", "Success"
    public string? UserId { get; set; } // null = system-wide notifications
    public bool IsRead { get; set; } = false;
    public DateTime? CreatedDate { get; set; }
    public DateTime? ReadDate { get; set; }
    public string? ActionUrl { get; set; } // Optional navigation link
    public string? Notes { get; set; }
}