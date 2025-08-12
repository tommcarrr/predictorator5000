using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Options;
using Resend;
using System.Linq;

namespace Predictorator.Services;

public class SubscriptionService
{
    private readonly IDataStore _store;
    private readonly IResend _resend;
    private readonly IConfiguration _config;
    private readonly ITwilioSmsSender _smsSender;
    private readonly IDateTimeProvider _dateTime;
    private readonly IBackgroundJobClient _jobs;
    private readonly EmailCssInliner _inliner;
    private readonly EmailTemplateRenderer _renderer;
    private readonly ILogger<SubscriptionService> _logger;

    private string AdminEmail =>
        _config["ADMIN_EMAIL"] ?? _config[$"{AdminUserOptions.SectionName}:Email"] ?? "admin@example.com";

    private async Task SendAdminEmailAsync(string subject, string body)
    {
        var html = _renderer.Render(body, _config["BASE_URL"] ?? string.Empty, null, preheader: body);
        var message = new EmailMessage
        {
            From = _config["Resend:From"] ?? "Prediction Fairy <no-reply@example.com>",
            Subject = subject,
            HtmlBody = _inliner.InlineCss(html)
        };
        message.To.Add(AdminEmail);
        await _resend.EmailSendAsync(message);
    }

    private async Task<bool> VerifyEmailSubscriberAsync(string token)
    {
        _logger.LogInformation("Checking email subscriber with verification token {Token}", token);
        var sub = await _store.GetEmailSubscriberByVerificationTokenAsync(token);
        if (sub == null)
        {
            _logger.LogInformation("Email subscriber with token {Token} not found", token);
            return false;
        }
        sub.IsVerified = true;
        await _store.UpdateEmailSubscriberAsync(sub);
        await SendAdminEmailAsync("Subscription confirmed", $"{sub.Email} confirmed their subscription.");
        return true;
    }

    private async Task<bool> VerifySmsSubscriberAsync(string token)
    {
        _logger.LogInformation("Checking SMS subscriber with verification token {Token}", token);
        var sub = await _store.GetSmsSubscriberByVerificationTokenAsync(token);
        if (sub == null)
        {
            _logger.LogInformation("SMS subscriber with token {Token} not found", token);
            return false;
        }
        sub.IsVerified = true;
        await _store.UpdateSmsSubscriberAsync(sub);
        await SendAdminEmailAsync("Subscription confirmed", $"{sub.PhoneNumber} confirmed their subscription.");
        return true;
    }

    private Task<int> CountExpiredEmailSubscribersAsync(DateTime cutoff) =>
        _store.CountExpiredEmailSubscribersAsync(cutoff);

    private Task<int> CountExpiredSmsSubscribersAsync(DateTime cutoff) =>
        _store.CountExpiredSmsSubscribersAsync(cutoff);

    private async Task<bool> UnsubscribeEmailSubscriberAsync(string token)
    {
        _logger.LogInformation("Attempting unsubscribe for email subscriber with token {Token}", token);
        var sub = await _store.GetEmailSubscriberByUnsubscribeTokenAsync(token);
        if (sub == null)
        {
            _logger.LogInformation("Email subscriber with token {Token} not found", token);
            return false;
        }
        await _store.RemoveEmailSubscriberAsync(sub);
        _logger.LogInformation("Email subscriber with token {Token} removed", token);
        return true;
    }

    private async Task<bool> UnsubscribeSmsSubscriberAsync(string token)
    {
        _logger.LogInformation("Attempting unsubscribe for SMS subscriber with token {Token}", token);
        var sub = await _store.GetSmsSubscriberByUnsubscribeTokenAsync(token);
        if (sub == null)
        {
            _logger.LogInformation("SMS subscriber with token {Token} not found", token);
            return false;
        }
        await _store.RemoveSmsSubscriberAsync(sub);
        _logger.LogInformation("SMS subscriber with token {Token} removed", token);
        return true;
    }
public SubscriptionService(
    IDataStore store,
    IResend resend,
    IConfiguration config,
    ITwilioSmsSender smsSender,
    IDateTimeProvider dateTime,
    IBackgroundJobClient jobs,
    EmailCssInliner inliner,
    EmailTemplateRenderer renderer,
    ILogger<SubscriptionService> logger)
{
    _store = store;
    _resend = resend;
    _config = config;
    _smsSender = smsSender;
    _dateTime = dateTime;
    _jobs = jobs;
    _inliner = inliner;
    _renderer = renderer;
    _logger = logger;
}

public Task SubscribeAsync(string? email, string? phoneNumber, string baseUrl)
{
    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(phoneNumber))
        throw new ArgumentException("Provide either an email or phone number, not both.");

    if (!string.IsNullOrWhiteSpace(email))
    {
        _logger.LogInformation("Scheduling email subscription for {Email}", email);
        var job = Job.FromExpression<SubscriptionService>(s => s.AddEmailSubscriberAsync(email, baseUrl));
        _jobs.Create(job, new EnqueuedState());
        return Task.CompletedTask;
    }

    if (!string.IsNullOrWhiteSpace(phoneNumber))
    {
        _logger.LogInformation("Scheduling SMS subscription for {PhoneNumber}", phoneNumber);
        var job = Job.FromExpression<SubscriptionService>(s => s.AddSmsSubscriberAsync(phoneNumber, baseUrl));
        _jobs.Create(job, new EnqueuedState());
        return Task.CompletedTask;
    }

    throw new ArgumentException("An email or phone number is required.");
}
    [AutomaticRetry(Attempts = 3)]
    public async Task AddEmailSubscriberAsync(string email, string baseUrl)
    {
        if (await _store.EmailSubscriberExistsAsync(email))
            return;

        var subscriber = new Subscriber
        {
            Email = email,
            IsVerified = false,
            VerificationToken = Guid.NewGuid().ToString("N"),
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = _dateTime.UtcNow
        };
        await _store.AddEmailSubscriberAsync(subscriber);

        await SendAdminEmailAsync("New subscriber", $"Email subscriber {email} added.");

        var verifyLink = $"{baseUrl}/Subscription/Verify?token={subscriber.VerificationToken}";

        var html = _renderer.Render("Please verify your email.", baseUrl, subscriber.UnsubscribeToken, "VERIFY EMAIL", verifyLink, preheader: "Please verify your email.");
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
        _logger.LogInformation("Attempting to add SMS subscriber {PhoneNumber}", phoneNumber);

        if (await _store.SmsSubscriberExistsAsync(phoneNumber))
        {
            _logger.LogInformation("SMS subscriber {PhoneNumber} already exists", phoneNumber);
            return;
        }

        var subscriber = new SmsSubscriber
        {
            PhoneNumber = phoneNumber,
            IsVerified = false,
            VerificationToken = Guid.NewGuid().ToString("N"),
            UnsubscribeToken = Guid.NewGuid().ToString("N"),
            CreatedAt = _dateTime.UtcNow
        };
        await _store.AddSmsSubscriberAsync(subscriber);
        _logger.LogInformation("SMS subscriber {PhoneNumber} stored with id {Id}", phoneNumber, subscriber.Id);

        await SendAdminEmailAsync("New subscriber", $"SMS subscriber {phoneNumber} added.");

        var verifyLink = $"{baseUrl}/Subscription/Verify?token={subscriber.VerificationToken}";
        var unsubscribeLink = $"{baseUrl}/Subscription/Unsubscribe?token={subscriber.UnsubscribeToken}";
        var message = $"Verify your phone subscription: {verifyLink}\n\n---\n\nTo unsubscribe: {unsubscribeLink}";
        await _smsSender.SendSmsAsync(phoneNumber, message);
        _logger.LogInformation("Verification SMS queued for {PhoneNumber}", phoneNumber);
    }

    public async Task<bool> VerifyAsync(string token)
    {
        _logger.LogInformation("Verifying subscriber with token {Token}", token);
        var result = await VerifyEmailSubscriberAsync(token)
            || await VerifySmsSubscriberAsync(token);
        _logger.LogInformation("Verification {Result} for token {Token}", result ? "succeeded" : "failed", token);
        return result;
    }

    public async Task<bool> UnsubscribeAsync(string token)
    {
        _logger.LogInformation("Unsubscribe requested for token {Token}", token);
        var result = await UnsubscribeEmailSubscriberAsync(token)
            || await UnsubscribeSmsSubscriberAsync(token);
        _logger.LogInformation("Unsubscribe {Result} for token {Token}", result ? "succeeded" : "failed", token);
        return result;
    }

    public async Task<int> CountExpiredUnverifiedAsync()
    {
        var cutoff = _dateTime.UtcNow.AddHours(-1);
        _logger.LogInformation("Checking for expired subscriptions before {Cutoff}", cutoff);

        var count = await CountExpiredEmailSubscribersAsync(cutoff);
        count += await CountExpiredSmsSubscribersAsync(cutoff);

        _logger.LogInformation("Total expired subscriptions found: {Count}", count);
        return count;
    }

    public async Task<bool> UnsubscribeByContactAsync(string contact)
    {
        if (string.IsNullOrWhiteSpace(contact))
            throw new ArgumentException("Contact is required", nameof(contact));

        if (contact.Contains('@'))
        {
            var normalized = contact.Trim().ToLowerInvariant();
            var subscriber = await _store.GetEmailSubscriberByEmailAsync(normalized);
            if (subscriber == null)
                return false;
            await _store.RemoveEmailSubscriberAsync(subscriber);
            return true;
        }
        else
        {
            var normalized = NormalizePhone(contact);
            _logger.LogInformation("Attempting unsubscribe by phone {Phone}", contact);
            var subs = await _store.GetSmsSubscribersAsync();
            var subscriber = subs.FirstOrDefault(s => NormalizePhone(s.PhoneNumber) == normalized);
            if (subscriber == null)
            {
                _logger.LogInformation("SMS subscriber {Phone} not found", contact);
                return false;
            }
            await _store.RemoveSmsSubscriberAsync(subscriber);
            _logger.LogInformation("SMS subscriber {Phone} removed", contact);
            return true;
        }
    }

    private static string NormalizePhone(string phone)
    {
        return string.Concat(phone.Where(char.IsDigit));
    }
}
