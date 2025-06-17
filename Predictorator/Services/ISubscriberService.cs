using Predictorator.Models;

namespace Predictorator.Services;

public interface ISubscriberService
{
    Task<Subscriber?> GetByTokenAsync(Guid token);
    Task<Subscriber> AddAsync(string email);
    Task VerifyAsync(Subscriber subscriber);
    Task UnsubscribeAsync(Subscriber subscriber);
}
