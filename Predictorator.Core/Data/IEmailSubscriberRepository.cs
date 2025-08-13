using Predictorator.Core.Models;

namespace Predictorator.Core.Data;

public interface IEmailSubscriberRepository
{
    Task<bool> EmailSubscriberExistsAsync(string email);
    Task AddEmailSubscriberAsync(Subscriber subscriber);
    Task<Subscriber?> GetEmailSubscriberByVerificationTokenAsync(string token);
    Task<Subscriber?> GetEmailSubscriberByUnsubscribeTokenAsync(string token);
    Task<int> CountExpiredEmailSubscribersAsync(DateTime cutoff);
    Task<Subscriber?> GetEmailSubscriberByEmailAsync(string normalizedEmail);
    Task<List<Subscriber>> GetEmailSubscribersAsync();
    Task<List<Subscriber>> GetVerifiedEmailSubscribersAsync();
    Task<Subscriber?> GetEmailSubscriberByIdAsync(int id);
    Task UpdateEmailSubscriberAsync(Subscriber subscriber);
    Task RemoveEmailSubscriberAsync(Subscriber subscriber);
}

