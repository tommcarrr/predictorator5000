namespace Predictorator.Core.Models;

public class SmsSubscriber : ISubscriber
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string VerificationToken { get; set; } = string.Empty;
    public string UnsubscribeToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
