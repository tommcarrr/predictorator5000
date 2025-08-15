using Predictorator.Core.Data;
using Microsoft.Extensions.Logging;
using Predictorator.Core.Models;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;

namespace Predictorator.Core.Services;

public class AdminSubscriberDto
{
    public int Id { get; set; }
    public string Contact { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string Type { get; set; } = string.Empty;

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
    private readonly IEmailSubscriberRepository _emails;
    private readonly ISmsSubscriberRepository _smsSubscribers;
    private readonly IConfiguration _config;
    private readonly NotificationService _notifications;
    private readonly ILogger<AdminService> _logger;
    private readonly IBackgroundJobService _jobs;
    private readonly IDateTimeProvider _time;
    private readonly CachePrefixService _prefix;
    private readonly INotificationSender<Subscriber> _emailSender;
    private readonly INotificationSender<SmsSubscriber> _smsSender;
    private readonly IBackgroundJobErrorService _errors;

    public AdminService(
        IEmailSubscriberRepository emails,
        ISmsSubscriberRepository smsSubscribers,
        IConfiguration config,
        NotificationService notifications,
        ILogger<AdminService> logger,
        IBackgroundJobService jobs,
        IDateTimeProvider time,
        CachePrefixService prefix,
        INotificationSender<Subscriber> emailSender,
        INotificationSender<SmsSubscriber> smsSender,
        IBackgroundJobErrorService errors)
    {
        _emails = emails;
        _smsSubscribers = smsSubscribers;
        _config = config;
        _notifications = notifications;
        _logger = logger;
        _jobs = jobs;
        _time = time;
        _prefix = prefix;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _errors = errors;
    }

    public async Task<List<AdminSubscriberDto>> GetSubscribersAsync()
    {
        _logger.LogInformation("Fetching SMS subscribers");
        var emails = (await _emails.GetEmailSubscribersAsync())
            .Select(s => new AdminSubscriberDto(s.Id, s.Email, s.IsVerified, "Email"))
            .ToList();
        var phones = (await _smsSubscribers.GetSmsSubscribersAsync())
            .Select(s => new AdminSubscriberDto(s.Id, s.PhoneNumber, s.IsVerified, "SMS"))
            .ToList();
        _logger.LogInformation("Fetched {Count} SMS subscribers", phones.Count);
        return emails.Concat(phones).OrderBy(s => s.Contact).ToList();
    }

    public async Task ConfirmAsync(string type, int id)
    {
        if (type == "Email")
        {
            var entity = await _emails.GetEmailSubscriberByIdAsync(id);
            if (entity != null)
            {
                entity.IsVerified = true;
                await _emails.UpdateEmailSubscriberAsync(entity);
            }
        }
        else
        {
            _logger.LogInformation("Confirming SMS subscriber {Id}", id);
            var entity = await _smsSubscribers.GetSmsSubscriberByIdAsync(id);
            if (entity != null)
            {
                entity.IsVerified = true;
                await _smsSubscribers.UpdateSmsSubscriberAsync(entity);
            }
        }
    }

    public async Task DeleteAsync(string type, int id)
    {
        if (type == "Email")
        {
            var entity = await _emails.GetEmailSubscriberByIdAsync(id);
            if (entity != null) await _emails.RemoveEmailSubscriberAsync(entity);
        }
        else
        {
            _logger.LogInformation("Deleting SMS subscriber {Id}", id);
            var entity = await _smsSubscribers.GetSmsSubscriberByIdAsync(id);
            if (entity != null) await _smsSubscribers.RemoveSmsSubscriberAsync(entity);
        }
    }

    public async Task<AdminSubscriberDto?> AddSubscriberAsync(string type, string contact)
    {
        if (type == "Email")
        {
            if (await _emails.EmailSubscriberExistsAsync(contact))
            {
                _logger.LogInformation("Email subscriber {Email} already exists", contact);
                return null;
            }
            var sub = new Subscriber
            {
                Email = contact,
                IsVerified = true,
                VerificationToken = Guid.NewGuid().ToString("N"),
                UnsubscribeToken = Guid.NewGuid().ToString("N"),
                CreatedAt = _time.UtcNow
            };
            await _emails.AddEmailSubscriberAsync(sub);
            _logger.LogInformation("Added email subscriber {Email} with id {Id}", sub.Email, sub.Id);
            return new AdminSubscriberDto(sub.Id, sub.Email, sub.IsVerified, "Email");
        }
        else
        {
            if (await _smsSubscribers.SmsSubscriberExistsAsync(contact))
            {
                _logger.LogInformation("SMS subscriber {Phone} already exists", contact);
                return null;
            }
            var sub = new SmsSubscriber
            {
                PhoneNumber = contact,
                IsVerified = true,
                VerificationToken = Guid.NewGuid().ToString("N"),
                UnsubscribeToken = Guid.NewGuid().ToString("N"),
                CreatedAt = _time.UtcNow
            };
            await _smsSubscribers.AddSmsSubscriberAsync(sub);
            _logger.LogInformation("Added SMS subscriber {Phone} with id {Id}", sub.PhoneNumber, sub.Id);
            return new AdminSubscriberDto(sub.Id, sub.PhoneNumber, sub.IsVerified, "SMS");
        }
    }

    public async Task<string> ExportSubscribersCsvAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Type,Contact,IsVerified,VerificationToken,UnsubscribeToken,CreatedAt");
        var emails = await _emails.GetEmailSubscribersAsync();
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

        var phones = await _smsSubscribers.GetSmsSubscribersAsync();
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
                if (await _emails.EmailSubscriberExistsAsync(contact)) continue;
                var sub = new Subscriber
                {
                    Email = contact,
                    IsVerified = isVerified,
                    VerificationToken = verify,
                    UnsubscribeToken = unsubscribe,
                    CreatedAt = created
                };
                await _emails.AddEmailSubscriberAsync(sub);
                added++;
            }
            else if (type == "SMS")
            {
                if (await _smsSubscribers.SmsSubscriberExistsAsync(contact)) continue;
                var sub = new SmsSubscriber
                {
                    PhoneNumber = contact,
                    IsVerified = isVerified,
                    VerificationToken = verify,
                    UnsubscribeToken = unsubscribe,
                    CreatedAt = created
                };
                await _smsSubscribers.AddSmsSubscriberAsync(sub);
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
                var sub = await _emails.GetEmailSubscriberByIdAsync(s.Id);
                if (sub != null)
                {
                    await _emailSender.SendAsync("This is a test notification.", baseUrl, sub);
                }
            }
            else
            {
                _logger.LogInformation("Sending test SMS to {Phone}", s.Contact);
                var sub = await _smsSubscribers.GetSmsSubscriberByIdAsync(s.Id);
                if (sub != null)
                {
                    await _smsSender.SendAsync("Test notification", baseUrl, sub);
                }
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
        var delay = TimeExtensions.ClampDelay(sendUtc, _time);
        return _jobs.ScheduleAsync(
            "SendSample",
            new { Recipients = recipients, Message = "Fixtures start in 2 hours!", BaseUrl = baseUrl },
            delay);
    }

    public Task ScheduleNewFixturesAsync(DateTime sendUtc)
    {
        var baseUrl = _config["BASE_URL"] ?? "http://localhost";
        var key = sendUtc.ToString("yyyy-MM-dd");
        var delay = TimeExtensions.ClampDelay(sendUtc, _time);
        return _jobs.ScheduleAsync(
            "SendNewFixturesAvailable",
            new { Key = key, BaseUrl = baseUrl },
            delay);
    }

    public Task ScheduleFixturesStartingSoonAsync(DateTime sendUtc)
    {
        var baseUrl = _config["BASE_URL"] ?? "http://localhost";
        var key = sendUtc.ToString("O");
        var delay = TimeExtensions.ClampDelay(sendUtc, _time);
        return _jobs.ScheduleAsync(
            "SendFixturesStartingSoon",
            new { Key = key, BaseUrl = baseUrl },
            delay);
    }

    public async Task<List<BackgroundJob>> GetJobsAsync()
    {
        var jobs = await _jobs.GetJobsAsync();
        return jobs.ToList();
    }

    public Task DeleteJobAsync(string id)
    {
        return _jobs.DeleteAsync(id);
    }

    public async Task<List<BackgroundJobError>> GetErrorsAsync()
    {
        var errors = await _errors.GetErrorsAsync();
        return errors.ToList();
    }

    public Task DeleteErrorAsync(string id)
    {
        return _errors.DeleteErrorAsync(id);
    }

    public Task ClearCachesAsync()
    {
        _prefix.Clear();
        return Task.CompletedTask;
    }
}
