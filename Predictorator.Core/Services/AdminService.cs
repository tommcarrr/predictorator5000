using Predictorator.Data;
using Microsoft.Extensions.Logging;
using Predictorator.Models;
using Resend;
using Hangfire;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;

namespace Predictorator.Services;

public class AdminSubscriberDto
{
    public int Id { get; set; }
    public string Contact { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string Type { get; set; } = string.Empty;

    // Parameterless constructor required for Hangfire deserialization
    public AdminSubscriberDto()
    {
    }

    public AdminSubscriberDto(int id, string contact, bool isVerified, string type)
    {
        Id = id;
        Contact = contact;
        IsVerified = isVerified;
        Type = type;
    }
}

public class AdminService
{
    private readonly IDataStore _store;
    private readonly IResend _resend;
    private readonly ITwilioSmsSender _sms;
    private readonly IConfiguration _config;
    private readonly EmailCssInliner _inliner;
    private readonly EmailTemplateRenderer _renderer;
    private readonly NotificationService _notifications;
    private readonly ILogger<AdminService> _logger;
    private readonly IBackgroundJobClient _jobs;
    private readonly IDateTimeProvider _time;
    private readonly CachePrefixService _prefix;

    public AdminService(
        IDataStore store,
        IResend resend,
        ITwilioSmsSender sms,
        IConfiguration config,
        EmailCssInliner inliner,
        EmailTemplateRenderer renderer,
        NotificationService notifications,
        ILogger<AdminService> logger,
        IBackgroundJobClient jobs,
        IDateTimeProvider time,
        CachePrefixService prefix)
    {
        _store = store;
        _resend = resend;
        _sms = sms;
        _config = config;
        _inliner = inliner;
        _renderer = renderer;
        _notifications = notifications;
        _logger = logger;
        _jobs = jobs;
        _time = time;
        _prefix = prefix;
    }

    public async Task<List<AdminSubscriberDto>> GetSubscribersAsync()
    {
        _logger.LogInformation("Fetching SMS subscribers");
        var emails = (await _store.GetEmailSubscribersAsync())
            .Select(s => new AdminSubscriberDto(s.Id, s.Email, s.IsVerified, "Email"))
            .ToList();
        var phones = (await _store.GetSmsSubscribersAsync())
            .Select(s => new AdminSubscriberDto(s.Id, s.PhoneNumber, s.IsVerified, "SMS"))
            .ToList();
        _logger.LogInformation("Fetched {Count} SMS subscribers", phones.Count);
        return emails.Concat(phones).OrderBy(s => s.Contact).ToList();
    }

    public async Task ConfirmAsync(string type, int id)
    {
        if (type == "Email")
        {
            var entity = await _store.GetEmailSubscriberByIdAsync(id);
            if (entity != null)
            {
                entity.IsVerified = true;
                await _store.UpdateEmailSubscriberAsync(entity);
            }
        }
        else
        {
            _logger.LogInformation("Confirming SMS subscriber {Id}", id);
            var entity = await _store.GetSmsSubscriberByIdAsync(id);
            if (entity != null)
            {
                entity.IsVerified = true;
                await _store.UpdateSmsSubscriberAsync(entity);
            }
        }
    }

    public async Task DeleteAsync(string type, int id)
    {
        if (type == "Email")
        {
            var entity = await _store.GetEmailSubscriberByIdAsync(id);
            if (entity != null) await _store.RemoveEmailSubscriberAsync(entity);
        }
        else
        {
            _logger.LogInformation("Deleting SMS subscriber {Id}", id);
            var entity = await _store.GetSmsSubscriberByIdAsync(id);
            if (entity != null) await _store.RemoveSmsSubscriberAsync(entity);
        }
    }

    public async Task<AdminSubscriberDto?> AddSubscriberAsync(string type, string contact)
    {
        if (type == "Email")
        {
            if (await _store.EmailSubscriberExistsAsync(contact))
                return null;
            var sub = new Subscriber
            {
                Email = contact,
                IsVerified = true,
                VerificationToken = Guid.NewGuid().ToString("N"),
                UnsubscribeToken = Guid.NewGuid().ToString("N"),
                CreatedAt = _time.UtcNow
            };
            await _store.AddEmailSubscriberAsync(sub);
            return new AdminSubscriberDto(sub.Id, sub.Email, sub.IsVerified, "Email");
        }
        else
        {
            if (await _store.SmsSubscriberExistsAsync(contact))
                return null;
            var sub = new SmsSubscriber
            {
                PhoneNumber = contact,
                IsVerified = true,
                VerificationToken = Guid.NewGuid().ToString("N"),
                UnsubscribeToken = Guid.NewGuid().ToString("N"),
                CreatedAt = _time.UtcNow
            };
            await _store.AddSmsSubscriberAsync(sub);
            return new AdminSubscriberDto(sub.Id, sub.PhoneNumber, sub.IsVerified, "SMS");
        }
    }

    public async Task<string> ExportSubscribersCsvAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Type,Contact,IsVerified,VerificationToken,UnsubscribeToken,CreatedAt");
        var emails = await _store.GetEmailSubscribersAsync();
        foreach (var e in emails)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                "Email",
                e.Email,
                e.IsVerified.ToString(CultureInfo.InvariantCulture),
                e.VerificationToken,
                e.UnsubscribeToken,
                e.CreatedAt.ToString("O", CultureInfo.InvariantCulture)
            }));
        }

        var phones = await _store.GetSmsSubscribersAsync();
        foreach (var p in phones)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                "SMS",
                p.PhoneNumber,
                p.IsVerified.ToString(CultureInfo.InvariantCulture),
                p.VerificationToken,
                p.UnsubscribeToken,
                p.CreatedAt.ToString("O", CultureInfo.InvariantCulture)
            }));
        }

        return sb.ToString();
    }

    public async Task<int> ImportSubscribersCsvAsync(Stream csv)
    {
        using var reader = new StreamReader(csv, Encoding.UTF8, leaveOpen: true);
        string? line;
        var added = 0;
        var first = true;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (first)
            {
                first = false;
                continue;
            }
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',', StringSplitOptions.None);
            if (parts.Length < 6) continue;

            var type = parts[0];
            var contact = parts[1];
            var isVerified = bool.TryParse(parts[2], out var v) && v;
            var verify = parts[3];
            var unsubscribe = parts[4];
            var created = DateTime.TryParse(parts[5], null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)
                ? dt
                : _time.UtcNow;

            if (type == "Email")
            {
                if (await _store.EmailSubscriberExistsAsync(contact)) continue;
                var sub = new Subscriber
                {
                    Email = contact,
                    IsVerified = isVerified,
                    VerificationToken = verify,
                    UnsubscribeToken = unsubscribe,
                    CreatedAt = created
                };
                await _store.AddEmailSubscriberAsync(sub);
                added++;
            }
            else if (type == "SMS")
            {
                if (await _store.SmsSubscriberExistsAsync(contact)) continue;
                var sub = new SmsSubscriber
                {
                    PhoneNumber = contact,
                    IsVerified = isVerified,
                    VerificationToken = verify,
                    UnsubscribeToken = unsubscribe,
                    CreatedAt = created
                };
                await _store.AddSmsSubscriberAsync(sub);
                added++;
            }
        }

        return added;
    }

    public async Task SendTestAsync(IEnumerable<AdminSubscriberDto> recipients)
    {
        var baseUrl = _config["BASE_URL"] ?? "http://localhost";
        foreach (var s in recipients)
        {
            if (s.Type == "Email")
            {
                var sub = await _store.GetEmailSubscriberByIdAsync(s.Id);
                if (sub != null)
                {
                    var html = _renderer.Render("This is a test notification.", baseUrl, sub.UnsubscribeToken, "VIEW FIXTURES", baseUrl, preheader: "This is a test notification.");
                    var message = new EmailMessage
                    {
                        From = _config["Resend:From"] ?? "Predictorator <noreply@example.com>",
                        Subject = "Test Notification",
                        HtmlBody = _inliner.InlineCss(html)
                    };
                    message.To.Add(s.Contact);
                    await _resend.EmailSendAsync(message);
                }
            }
            else
            {
                _logger.LogInformation("Sending test SMS to {Phone}", s.Contact);
                await _sms.SendSmsAsync(s.Contact, "Test notification");
            }
        }
    }

    public async Task SendNewFixturesSampleAsync(IEnumerable<AdminSubscriberDto> recipients)
    {
        var baseUrl = _config["BASE_URL"] ?? "http://localhost";
        await _notifications.SendSampleAsync(recipients, "Fixtures start today!", baseUrl);
    }

    public async Task SendFixturesStartingSoonSampleAsync(IEnumerable<AdminSubscriberDto> recipients)
    {
        var baseUrl = _config["BASE_URL"] ?? "http://localhost";
        await _notifications.SendSampleAsync(recipients, "Fixtures start in 2 hours!", baseUrl);
    }

    public Task ScheduleFixturesStartingSoonSampleAsync(IEnumerable<AdminSubscriberDto> recipients, DateTime sendUtc)
    {
        var baseUrl = _config["BASE_URL"] ?? "http://localhost";
        var delay = sendUtc - _time.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
        _jobs.Schedule<NotificationService>(s => s.SendSampleAsync(recipients, "Fixtures start in 2 hours!", baseUrl), delay);
        return Task.CompletedTask;
    }

    public Task ScheduleNewFixturesAsync(DateTime sendUtc)
    {
        var baseUrl = _config["BASE_URL"] ?? "http://localhost";
        var key = sendUtc.ToString("yyyy-MM-dd");
        var delay = sendUtc - _time.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
        _jobs.Schedule<NotificationService>(s => s.SendNewFixturesAvailableAsync(key, baseUrl), delay);
        return Task.CompletedTask;
    }

    public Task ScheduleFixturesStartingSoonAsync(DateTime sendUtc)
    {
        var baseUrl = _config["BASE_URL"] ?? "http://localhost";
        var key = sendUtc.ToString("O");
        var delay = sendUtc - _time.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
        _jobs.Schedule<NotificationService>(s => s.SendFixturesStartingSoonAsync(key, baseUrl), delay);
        return Task.CompletedTask;
    }

    public Task ClearCachesAsync()
    {
        _prefix.Clear();
        return Task.CompletedTask;
    }
}
