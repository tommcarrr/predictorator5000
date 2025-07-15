using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Predictorator.Data;
using Predictorator.Models;
using Resend;

namespace Predictorator.Services;

public class SubscriptionService
{
    private readonly ApplicationDbContext _db;
    private readonly IResend _resend;
    private readonly IConfiguration _config;
    private readonly ITwilioSmsSender _smsSender;
    private readonly IDateTimeProvider _dateTime;
    private readonly IBackgroundJobClient _jobs;

    public SubscriptionService(ApplicationDbContext db, IResend resend, IConfiguration config, ITwilioSmsSender smsSender, IDateTimeProvider dateTime, IBackgroundJobClient jobs)
    {
        _db = db;
        _resend = resend;
        _config = config;
        _smsSender = smsSender;
        _dateTime = dateTime;
        _jobs = jobs;
    }

    public Task SubscribeAsync(string? email, string? phoneNumber, string baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Provide either an email or phone number, not both.");

        if (!string.IsNullOrWhiteSpace(email))
        {
            var job = Job.FromExpression<SubscriptionService>(s => s.AddEmailSubscriberAsync(email, baseUrl));
            _jobs.Create(job, new EnqueuedState());
            return Task.CompletedTask;
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var job = Job.FromExpression<SubscriptionService>(s => s.AddSmsSubscriberAsync(phoneNumber, baseUrl));
            _jobs.Create(job, new EnqueuedState());
            return Task.CompletedTask;
        }

        throw new ArgumentException("An email or phone number is required.");
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task AddEmailSubscriberAsync(string email, string baseUrl)
    {
        if (await _db.Subscribers.AnyAsync(s => s.Email == email))
            return;

        var subscriber = new Subscriber
        {
            Email = email,
            IsVerified = false,
            VerificationToken = Guid.NewGuid().ToString("N"),
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = _dateTime.UtcNow
        };
        _db.Subscribers.Add(subscriber);
        await _db.SaveChangesAsync();

        var verifyLink = $"{baseUrl}/Subscription/Verify?token={subscriber.VerificationToken}";
        var unsubscribeLink = $"{baseUrl}/Subscription/Unsubscribe?token={subscriber.UnsubscribeToken}";

        var message = new EmailMessage
        {
            From = _config["Resend:From"] ?? "no-reply@example.com",
            Subject = "Verify your email",
            HtmlBody = $"<p>Please <a href=\"{verifyLink}\">verify your email</a>.</p><p>If you did not request this, you can <a href=\"{unsubscribeLink}\">unsubscribe</a>.</p>"
        };
        message.To.Add(email);

        await _resend.EmailSendAsync(message);
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task AddSmsSubscriberAsync(string phoneNumber, string baseUrl)
    {
        if (await _db.SmsSubscribers.AnyAsync(s => s.PhoneNumber == phoneNumber))
            return;

        var subscriber = new SmsSubscriber
        {
            PhoneNumber = phoneNumber,
            IsVerified = false,
            VerificationToken = Guid.NewGuid().ToString("N"),
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = _dateTime.UtcNow
        };
        _db.SmsSubscribers.Add(subscriber);
        await _db.SaveChangesAsync();

        var verifyLink = $"{baseUrl}/Subscription/Verify?token={subscriber.VerificationToken}";
        var unsubscribeLink = $"{baseUrl}/Subscription/Unsubscribe?token={subscriber.UnsubscribeToken}";
        var message = $"Verify your phone subscription: {verifyLink}. To unsubscribe: {unsubscribeLink}";
        await _smsSender.SendSmsAsync(phoneNumber, message);
    }

    public async Task<bool> VerifyAsync(string token)
    {
        var subscriber = await _db.Subscribers.FirstOrDefaultAsync(s => s.VerificationToken == token);
        if (subscriber != null)
        {
            subscriber.IsVerified = true;
            await _db.SaveChangesAsync();
            return true;
        }

        var smsSubscriber = await _db.SmsSubscribers.FirstOrDefaultAsync(s => s.VerificationToken == token);
        if (smsSubscriber != null)
        {
            smsSubscriber.IsVerified = true;
            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<bool> UnsubscribeAsync(string token)
    {
        var subscriber = await _db.Subscribers.FirstOrDefaultAsync(s => s.UnsubscribeToken == token);
        if (subscriber != null)
        {
            _db.Subscribers.Remove(subscriber);
            await _db.SaveChangesAsync();
            return true;
        }

        var smsSubscriber = await _db.SmsSubscribers.FirstOrDefaultAsync(s => s.UnsubscribeToken == token);
        if (smsSubscriber != null)
        {
            _db.SmsSubscribers.Remove(smsSubscriber);
            await _db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task RemoveExpiredUnverifiedAsync()
    {
        var cutoff = _dateTime.UtcNow.AddHours(-1);

        var expiredEmail = await _db.Subscribers
            .Where(s => !s.IsVerified && s.CreatedAt < cutoff)
            .ToListAsync();
        if (expiredEmail.Any())
            _db.Subscribers.RemoveRange(expiredEmail);

        var expiredSms = await _db.SmsSubscribers
            .Where(s => !s.IsVerified && s.CreatedAt < cutoff)
            .ToListAsync();
        if (expiredSms.Any())
            _db.SmsSubscribers.RemoveRange(expiredSms);

        if (expiredEmail.Any() || expiredSms.Any())
            await _db.SaveChangesAsync();
    }
}

