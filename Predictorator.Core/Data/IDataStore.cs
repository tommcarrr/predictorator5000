using Predictorator.Core.Models;

namespace Predictorator.Core.Data;

public interface IDataStore
{
    // Email subscribers
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

    // SMS subscribers
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

    // Sent notifications
    Task<bool> SentNotificationExistsAsync(string type, string key);
    Task AddSentNotificationAsync(SentNotification notification);
}
