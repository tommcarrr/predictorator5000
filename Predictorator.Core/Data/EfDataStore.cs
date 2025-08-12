using Microsoft.EntityFrameworkCore;
using Predictorator.Models;

namespace Predictorator.Data;

public class EfDataStore : IDataStore
{
    private readonly ApplicationDbContext _db;

    public EfDataStore(ApplicationDbContext db)
    {
        _db = db;
    }

    // Email subscribers
    public Task<bool> EmailSubscriberExistsAsync(string email) =>
        _db.Subscribers.AnyAsync(s => s.Email == email);

    public async Task AddEmailSubscriberAsync(Subscriber subscriber)
    {
        _db.Subscribers.Add(subscriber);
        await _db.SaveChangesAsync();
    }

    public Task<Subscriber?> GetEmailSubscriberByVerificationTokenAsync(string token) =>
        _db.Subscribers.FirstOrDefaultAsync(s => s.VerificationToken == token);

    public Task<Subscriber?> GetEmailSubscriberByUnsubscribeTokenAsync(string token) =>
        _db.Subscribers.FirstOrDefaultAsync(s => s.UnsubscribeToken == token);

    public Task<int> CountExpiredEmailSubscribersAsync(DateTime cutoff) =>
        _db.Subscribers.CountAsync(s => !s.IsVerified && s.CreatedAt < cutoff);

    public Task<Subscriber?> GetEmailSubscriberByEmailAsync(string normalizedEmail) =>
        _db.Subscribers.FirstOrDefaultAsync(s => s.Email.ToLower() == normalizedEmail);

    public Task<List<Subscriber>> GetEmailSubscribersAsync() =>
        _db.Subscribers.ToListAsync();

    public Task<List<Subscriber>> GetVerifiedEmailSubscribersAsync() =>
        _db.Subscribers.Where(s => s.IsVerified).ToListAsync();

    public Task<Subscriber?> GetEmailSubscriberByIdAsync(int id) =>
        _db.Subscribers.FirstOrDefaultAsync(s => s.Id == id);

    public async Task UpdateEmailSubscriberAsync(Subscriber subscriber)
    {
        _db.Subscribers.Update(subscriber);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveEmailSubscriberAsync(Subscriber subscriber)
    {
        _db.Subscribers.Remove(subscriber);
        await _db.SaveChangesAsync();
    }

    // SMS subscribers
    public Task<bool> SmsSubscriberExistsAsync(string phoneNumber) =>
        _db.SmsSubscribers.AnyAsync(s => s.PhoneNumber == phoneNumber);

    public async Task AddSmsSubscriberAsync(SmsSubscriber subscriber)
    {
        _db.SmsSubscribers.Add(subscriber);
        await _db.SaveChangesAsync();
    }

    public Task<SmsSubscriber?> GetSmsSubscriberByVerificationTokenAsync(string token) =>
        _db.SmsSubscribers.FirstOrDefaultAsync(s => s.VerificationToken == token);

    public Task<SmsSubscriber?> GetSmsSubscriberByUnsubscribeTokenAsync(string token) =>
        _db.SmsSubscribers.FirstOrDefaultAsync(s => s.UnsubscribeToken == token);

    public Task<int> CountExpiredSmsSubscribersAsync(DateTime cutoff) =>
        _db.SmsSubscribers.CountAsync(s => !s.IsVerified && s.CreatedAt < cutoff);

    public Task<List<SmsSubscriber>> GetSmsSubscribersAsync() =>
        _db.SmsSubscribers.ToListAsync();

    public Task<List<SmsSubscriber>> GetVerifiedSmsSubscribersAsync() =>
        _db.SmsSubscribers.Where(s => s.IsVerified).ToListAsync();

    public Task<SmsSubscriber?> GetSmsSubscriberByIdAsync(int id) =>
        _db.SmsSubscribers.FirstOrDefaultAsync(s => s.Id == id);

    public async Task UpdateSmsSubscriberAsync(SmsSubscriber subscriber)
    {
        _db.SmsSubscribers.Update(subscriber);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveSmsSubscriberAsync(SmsSubscriber subscriber)
    {
        _db.SmsSubscribers.Remove(subscriber);
        await _db.SaveChangesAsync();
    }

    // Sent notifications
    public Task<bool> SentNotificationExistsAsync(string type, string key) =>
        _db.SentNotifications.AnyAsync(n => n.Type == type && n.Key == key);

    public async Task AddSentNotificationAsync(SentNotification notification)
    {
        _db.SentNotifications.Add(notification);
        await _db.SaveChangesAsync();
    }
}
