namespace Predictorator.Models;

public interface ISubscriber
{
    bool IsVerified { get; set; }
    string VerificationToken { get; set; }
    string UnsubscribeToken { get; set; }
    DateTime CreatedAt { get; set; }
}
