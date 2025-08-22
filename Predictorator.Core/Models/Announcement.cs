namespace Predictorator.Core.Models;

public class Announcement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime ExpiresAt { get; set; }
}
