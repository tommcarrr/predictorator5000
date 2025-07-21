using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Microsoft.Extensions.Logging;
using Predictorator.Models;
using Resend;
using Hangfire;

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
    private readonly ApplicationDbContext _db;
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
        ApplicationDbContext db,
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
        _db = db;
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
        var emails = await _db.Subscribers
            .Select(s => new AdminSubscriberDto(s.Id, s.Email, s.IsVerified, "Email"))
            .ToListAsync();
        var phones = await _db.SmsSubscribers
            .Select(s => new AdminSubscriberDto(s.Id, s.PhoneNumber, s.IsVerified, "SMS"))
            .ToListAsync();
        _logger.LogInformation("Fetched {Count} SMS subscribers", phones.Count);
        return emails.Concat(phones).OrderBy(s => s.Contact).ToList();
    }

    public async Task ConfirmAsync(string type, int id)
    {
        if (type == "Email")
        {
            var entity = await _db.Subscribers.FindAsync(id);
            if (entity != null) entity.IsVerified = true;
        }
        else
        {
            _logger.LogInformation("Confirming SMS subscriber {Id}", id);
            var entity = await _db.SmsSubscribers.FindAsync(id);
            if (entity != null) entity.IsVerified = true;
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string type, int id)
    {
        if (type == "Email")
        {
            var entity = await _db.Subscribers.FindAsync(id);
            if (entity != null) _db.Subscribers.Remove(entity);
        }
        else
        {
            _logger.LogInformation("Deleting SMS subscriber {Id}", id);
            var entity = await _db.SmsSubscribers.FindAsync(id);
            if (entity != null) _db.SmsSubscribers.Remove(entity);
        }
        await _db.SaveChangesAsync();
    }

    public async Task SendTestAsync(IEnumerable<AdminSubscriberDto> recipients)
    {
        var baseUrl = _config["BASE_URL"] ?? "http://localhost";
        foreach (var s in recipients)
        {
            if (s.Type == "Email")
            {
                var sub = await _db.Subscribers.FirstOrDefaultAsync(x => x.Id == s.Id);
                if (sub != null)
                {
                    var html = _renderer.Render("This is a test notification.", baseUrl, sub.UnsubscribeToken, "VIEW FIXTURES", baseUrl);
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

    public Task ClearCachesAsync()
    {
        _prefix.Clear();
        return Task.CompletedTask;
    }
}
