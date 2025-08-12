using Predictorator.Data;
using Predictorator.Models;

namespace Predictorator.Tests.Helpers;

public class InMemoryDataStore : IDataStore
{
    public List<Subscriber> EmailSubscribers { get; } = new();
    public List<SmsSubscriber> SmsSubscribers { get; } = new();
    public List<SentNotification> SentNotifications { get; } = new();
    private int _emailId = 1;
    private int _smsId = 1;

    // Email subscribers
    public Task<bool> EmailSubscriberExistsAsync(string email) =>
        Task.FromResult(EmailSubscribers.Any(s => s.Email == email));

    public Task AddEmailSubscriberAsync(Subscriber subscriber)
    {
        subscriber.Id = _emailId++;
        EmailSubscribers.Add(subscriber);
        return Task.CompletedTask;
    }

    public Task<Subscriber?> GetEmailSubscriberByVerificationTokenAsync(string token) =>
        Task.FromResult(EmailSubscribers.FirstOrDefault(s => s.VerificationToken == token));

    public Task<Subscriber?> GetEmailSubscriberByUnsubscribeTokenAsync(string token) =>
        Task.FromResult(EmailSubscribers.FirstOrDefault(s => s.UnsubscribeToken == token));

    public Task<int> CountExpiredEmailSubscribersAsync(DateTime cutoff) =>
        Task.FromResult(EmailSubscribers.Count(s => !s.IsVerified && s.CreatedAt < cutoff));

    public Task<Subscriber?> GetEmailSubscriberByEmailAsync(string normalizedEmail) =>
        Task.FromResult(EmailSubscribers.FirstOrDefault(s => s.Email.ToLower() == normalizedEmail));

    public Task<List<Subscriber>> GetEmailSubscribersAsync() =>
        Task.FromResult(EmailSubscribers.ToList());

    public Task<List<Subscriber>> GetVerifiedEmailSubscribersAsync() =>
        Task.FromResult(EmailSubscribers.Where(s => s.IsVerified).ToList());

    public Task<Subscriber?> GetEmailSubscriberByIdAsync(int id) =>
        Task.FromResult(EmailSubscribers.FirstOrDefault(s => s.Id == id));

    public Task UpdateEmailSubscriberAsync(Subscriber subscriber)
    {
        var existing = EmailSubscribers.First(s => s.Id == subscriber.Id);
        existing.Email = subscriber.Email;
        existing.IsVerified = subscriber.IsVerified;
        existing.VerificationToken = subscriber.VerificationToken;
        existing.UnsubscribeToken = subscriber.UnsubscribeToken;
        existing.CreatedAt = subscriber.CreatedAt;
        return Task.CompletedTask;
    }

    public Task RemoveEmailSubscriberAsync(Subscriber subscriber)
    {
        EmailSubscribers.RemoveAll(s => s.Id == subscriber.Id);
        return Task.CompletedTask;
    }

    // SMS subscribers
    public Task<bool> SmsSubscriberExistsAsync(string phoneNumber) =>
        Task.FromResult(SmsSubscribers.Any(s => s.PhoneNumber == phoneNumber));

    public Task AddSmsSubscriberAsync(SmsSubscriber subscriber)
    {
        subscriber.Id = _smsId++;
        SmsSubscribers.Add(subscriber);
        return Task.CompletedTask;
    }

    public Task<SmsSubscriber?> GetSmsSubscriberByVerificationTokenAsync(string token) =>
        Task.FromResult(SmsSubscribers.FirstOrDefault(s => s.VerificationToken == token));

    public Task<SmsSubscriber?> GetSmsSubscriberByUnsubscribeTokenAsync(string token) =>
        Task.FromResult(SmsSubscribers.FirstOrDefault(s => s.UnsubscribeToken == token));

    public Task<int> CountExpiredSmsSubscribersAsync(DateTime cutoff) =>
        Task.FromResult(SmsSubscribers.Count(s => !s.IsVerified && s.CreatedAt < cutoff));

    public Task<List<SmsSubscriber>> GetSmsSubscribersAsync() =>
        Task.FromResult(SmsSubscribers.ToList());

    public Task<List<SmsSubscriber>> GetVerifiedSmsSubscribersAsync() =>
        Task.FromResult(SmsSubscribers.Where(s => s.IsVerified).ToList());

    public Task<SmsSubscriber?> GetSmsSubscriberByIdAsync(int id) =>
        Task.FromResult(SmsSubscribers.FirstOrDefault(s => s.Id == id));

    public Task UpdateSmsSubscriberAsync(SmsSubscriber subscriber)
    {
        var existing = SmsSubscribers.First(s => s.Id == subscriber.Id);
        existing.PhoneNumber = subscriber.PhoneNumber;
        existing.IsVerified = subscriber.IsVerified;
        existing.VerificationToken = subscriber.VerificationToken;
        existing.UnsubscribeToken = subscriber.UnsubscribeToken;
        existing.CreatedAt = subscriber.CreatedAt;
        return Task.CompletedTask;
    }

    public Task RemoveSmsSubscriberAsync(SmsSubscriber subscriber)
    {
        SmsSubscribers.RemoveAll(s => s.Id == subscriber.Id);
        return Task.CompletedTask;
    }

    // Sent notifications
    public Task<bool> SentNotificationExistsAsync(string type, string key) =>
        Task.FromResult(SentNotifications.Any(n => n.Type == type && n.Key == key));

    public Task AddSentNotificationAsync(SentNotification notification)
    {
        SentNotifications.Add(notification);
        return Task.CompletedTask;
    }
}
