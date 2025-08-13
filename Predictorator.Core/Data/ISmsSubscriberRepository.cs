using Predictorator.Core.Models;

namespace Predictorator.Core.Data;

public interface ISmsSubscriberRepository
{
    Task<bool> SmsSubscriberExistsAsync(string phoneNumber);
    Task AddSmsSubscriberAsync(SmsSubscriber subscriber);
    Task<SmsSubscriber?> GetSmsSubscriberByVerificationTokenAsync(string token);
    Task<SmsSubscriber?> GetSmsSubscriberByUnsubscribeTokenAsync(string token);
    Task<int> CountExpiredSmsSubscribersAsync(DateTime cutoff);
    Task<List<SmsSubscriber>> GetSmsSubscribersAsync();
    Task<List<SmsSubscriber>> GetVerifiedSmsSubscribersAsync();
    Task<SmsSubscriber?> GetSmsSubscriberByIdAsync(int id);
    Task UpdateSmsSubscriberAsync(SmsSubscriber subscriber);
    Task RemoveSmsSubscriberAsync(SmsSubscriber subscriber);
}

