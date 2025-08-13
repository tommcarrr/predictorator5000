namespace Predictorator.Core.Models;

public interface ISubscriber
{
    int Id { get; set; }
    bool IsVerified { get; set; }
    string VerificationToken { get; set; }
    string UnsubscribeToken { get; set; }
    DateTime CreatedAt { get; set; }
}
