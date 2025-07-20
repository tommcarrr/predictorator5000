using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Options;
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
    private readonly EmailCssInliner _inliner;
    private readonly EmailTemplateRenderer _renderer;

    private string AdminEmail =>
        _config["ADMIN_EMAIL"] ?? _config[$"{AdminUserOptions.SectionName}:Email"] ?? "admin@example.com";

    private async Task SendAdminEmailAsync(string subject, string body)
    {
        var html = _renderer.Render(body, _config["BASE_URL"] ?? string.Empty, null);
        var message = new EmailMessage
        {
            From = _config["Resend:From"] ?? "Prediction Fairy <no-reply@example.com>",
            Subject = subject,
            HtmlBody = _inliner.InlineCss(html)
        };
        message.To.Add(AdminEmail);
        await _resend.EmailSendAsync(message);
    }

    private async Task<bool> VerifySubscriberAsync<TEntity>(DbSet<TEntity> set, string token) where TEntity : class, ISubscriber
    {
        var entity = await set.FirstOrDefaultAsync(s => s.VerificationToken == token);
        if (entity == null) return false;
        entity.IsVerified = true;
        await _db.SaveChangesAsync();

        var contact = entity switch
        {
            Subscriber s => s.Email,
            SmsSubscriber sms => sms.PhoneNumber,
            _ => ""
        };
        await SendAdminEmailAsync("Subscription confirmed", $"{contact} confirmed their subscription.");

        return true;
    }

    private async Task<bool> UnsubscribeSubscriberAsync<TEntity>(DbSet<TEntity> set, string token) where TEntity : class, ISubscriber
    {
        var entity = await set.FirstOrDefaultAsync(s => s.UnsubscribeToken == token);
        if (entity == null) return false;
        set.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    private async Task<bool> RemoveExpiredSubscribersAsync<TEntity>(DbSet<TEntity> set, DateTime cutoff) where TEntity : class, ISubscriber
    {
        var expired = await set
            .Where(s => !s.IsVerified && s.CreatedAt < cutoff)
            .ToListAsync();
        if (!expired.Any()) return false;
        set.RemoveRange(expired);
        return true;
    }

    public SubscriptionService(ApplicationDbContext db, IResend resend, IConfiguration config, ITwilioSmsSender smsSender, IDateTimeProvider dateTime, IBackgroundJobClient jobs, EmailCssInliner inliner, EmailTemplateRenderer renderer)
    {
        _db = db;
        _resend = resend;
        _config = config;
        _smsSender = smsSender;
        _dateTime = dateTime;
        _jobs = jobs;
        _inliner = inliner;
        _renderer = renderer;
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

        await SendAdminEmailAsync("New subscriber", $"Email subscriber {email} added.");

        var verifyLink = $"{baseUrl}/Subscription/Verify?token={subscriber.VerificationToken}";

        var html = _renderer.Render("Please verify your email.", baseUrl, subscriber.UnsubscribeToken, "VERIFY EMAIL", verifyLink);
        var message = new EmailMessage
        {
            From = _config["Resend:From"] ?? "Prediction Fairy <no-reply@example.com>",
            Subject = "Verify your email",
            HtmlBody = _inliner.InlineCss(html)
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

        await SendAdminEmailAsync("New subscriber", $"SMS subscriber {phoneNumber} added.");

        var verifyLink = $"{baseUrl}/Subscription/Verify?token={subscriber.VerificationToken}";
        var unsubscribeLink = $"{baseUrl}/Subscription/Unsubscribe?token={subscriber.UnsubscribeToken}";
        var message = $"Verify your phone subscription: {verifyLink}. To unsubscribe: {unsubscribeLink}";
        await _smsSender.SendSmsAsync(phoneNumber, message);
    }

    public async Task<bool> VerifyAsync(string token)
    {
        return await VerifySubscriberAsync(_db.Subscribers, token)
            || await VerifySubscriberAsync(_db.SmsSubscribers, token);
    }

    public async Task<bool> UnsubscribeAsync(string token)
    {
        return await UnsubscribeSubscriberAsync(_db.Subscribers, token)
            || await UnsubscribeSubscriberAsync(_db.SmsSubscribers, token);
    }

    public async Task RemoveExpiredUnverifiedAsync()
    {
        var cutoff = _dateTime.UtcNow.AddHours(-1);

        var removed = await RemoveExpiredSubscribersAsync(_db.Subscribers, cutoff);
        removed |= await RemoveExpiredSubscribersAsync(_db.SmsSubscribers, cutoff);

        if (removed)
            await _db.SaveChangesAsync();
    }

    public async Task<bool> UnsubscribeByContactAsync(string contact)
    {
        if (string.IsNullOrWhiteSpace(contact))
            throw new ArgumentException("Contact is required", nameof(contact));

        if (contact.Contains('@'))
        {
            var normalized = contact.Trim().ToLowerInvariant();
            var subscriber = await _db.Subscribers
                .FirstOrDefaultAsync(s => s.Email.ToLower() == normalized);
            if (subscriber == null)
                return false;
            _db.Subscribers.Remove(subscriber);
            await _db.SaveChangesAsync();
            return true;
        }
        else
        {
            var normalized = NormalizePhone(contact);
            var subs = await _db.SmsSubscribers.ToListAsync();
            var subscriber = subs.FirstOrDefault(s => NormalizePhone(s.PhoneNumber) == normalized);
            if (subscriber == null)
                return false;
            _db.SmsSubscribers.Remove(subscriber);
            await _db.SaveChangesAsync();
            return true;
        }
    }

    private static string NormalizePhone(string phone)
    {
        return string.Concat(phone.Where(char.IsDigit));
    }
}

