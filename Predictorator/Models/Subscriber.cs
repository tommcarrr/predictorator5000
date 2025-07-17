namespace Predictorator.Models;

public class Subscriber : ISubscriber
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string VerificationToken { get; set; } = string.Empty;
    public string UnsubscribeToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
