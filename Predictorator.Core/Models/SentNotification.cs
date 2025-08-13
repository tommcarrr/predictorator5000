namespace Predictorator.Core.Models;

public class SentNotification
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
