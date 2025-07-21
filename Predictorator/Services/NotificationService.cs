using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Predictorator.Data;
using Predictorator.Models;
using Resend;

namespace Predictorator.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IResend _resend;
    private readonly ITwilioSmsSender _sms;
    private readonly IConfiguration _config;
    private readonly IFixtureService _fixtures;
    private readonly IDateRangeCalculator _range;
    private readonly NotificationFeatureService _features;
    private readonly IDateTimeProvider _time;
    private readonly IBackgroundJobClient _jobs;
    private readonly EmailCssInliner _inliner;
    private readonly EmailTemplateRenderer _renderer;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext db,
        IResend resend,
        ITwilioSmsSender sms,
        IConfiguration config,
        IFixtureService fixtures,
        IDateRangeCalculator range,
        NotificationFeatureService features,
        IDateTimeProvider time,
        IBackgroundJobClient jobs,
        EmailCssInliner inliner,
        EmailTemplateRenderer renderer,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _resend = resend;
        _sms = sms;
        _config = config;
        _fixtures = fixtures;
        _range = range;
        _features = features;
        _time = time;
        _jobs = jobs;
        _inliner = inliner;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task CheckFixturesAsync()
    {
        if (!_features.AnyEnabled)
            return;

        var baseUrl = _config["BASE_URL"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return;

        var (from, to) = _range.GetDates(null, null, null);
        var response = await _fixtures.GetFixturesAsync(from, to);
        if (response.Response.Count == 0)
            return;

        var nowUtc = _time.UtcNow;
        var ukTz = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var nowUk = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ukTz);
        var future = response.Response
            .Where(f => f.Fixture.Date.ToUniversalTime() > nowUtc)
            .OrderBy(f => f.Fixture.Date)
            .FirstOrDefault();
        if (future != null)
        {
            var key = future.Fixture.Date.Date.ToString("yyyy-MM-dd");
            var sent = await _db.SentNotifications
                .AnyAsync(n => n.Type == "NewFixtures" && n.Key == key);
            if (!sent)
            {
                var sendTimeUk = nowUk.Date.AddHours(10);
                var sendTimeUtc = TimeZoneInfo.ConvertTimeToUtc(sendTimeUk, ukTz);
                var delay = sendTimeUtc - nowUtc;
                if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
                _jobs.Schedule<NotificationService>(
                    s => s.SendNewFixturesAvailableAsync(key, baseUrl),
                    delay);
            }
        }

        var first = response.Response.OrderBy(f => f.Fixture.Date).First();
        var firstUk = TimeZoneInfo.ConvertTime(first.Fixture.Date, ukTz);
        if (firstUk.Date == nowUk.Date)
        {
            var key = first.Fixture.Date.ToString("O");
            var sent = await _db.SentNotifications
                .AnyAsync(n => n.Type == "FixturesStartingSoon" && n.Key == key);
            if (!sent)
            {
                var sendTimeUtc = first.Fixture.Date.AddHours(-1);
                var delay = sendTimeUtc - nowUtc;
                if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;
                _jobs.Schedule<NotificationService>(
                    s => s.SendFixturesStartingSoonAsync(key, baseUrl),
                    delay);
            }
        }
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendNewFixturesAvailableAsync(string key, string baseUrl)
    {
        await SendToAllAsync("New fixtures are available!", baseUrl, "NewFixtures", key);
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task SendFixturesStartingSoonAsync(string key, string baseUrl)
    {
        await SendToAllAsync("Fixtures start in 1 hour!", baseUrl, "FixturesStartingSoon", key);
    }

    private EmailMessage CreateEmailMessage(string message, string baseUrl, Subscriber sub)
    {
        var html = _renderer.Render(message, baseUrl, sub.UnsubscribeToken, "VIEW FIXTURES", baseUrl);
        var emailMessage = new EmailMessage
        {
            From = _config["Resend:From"] ?? "Prediction Fairy <no-reply@example.com>",
            Subject = "Predictorator Notification",
            HtmlBody = _inliner.InlineCss(html)
        };
        emailMessage.To.Add(sub.Email);
        return emailMessage;
    }

    private string CreateSmsMessage(string message, string baseUrl, SmsSubscriber sub)
    {
        var link = $"{baseUrl}/Subscription/Unsubscribe?token={sub.UnsubscribeToken}";
        return $"{message} {baseUrl}\n\n---\n\nUnsubscribe: {link}";
    }

    public async Task SendNotificationAsync(string message, string baseUrl, Subscriber sub)
    {
        var emailMessage = CreateEmailMessage(message, baseUrl, sub);
        await _resend.EmailSendAsync(emailMessage);
    }

    public async Task SendNotificationAsync(string message, string baseUrl, SmsSubscriber sub)
    {
        var smsMessage = CreateSmsMessage(message, baseUrl, sub);
        await _sms.SendSmsAsync(sub.PhoneNumber, smsMessage);
    }

    public async Task SendSampleAsync(IEnumerable<AdminSubscriberDto> recipients, string message, string baseUrl)
    {
        foreach (var r in recipients)
        {
            if (r.Type == "Email")
            {
                var sub = await _db.Subscribers.FirstOrDefaultAsync(s => s.Id == r.Id);
                if (sub != null)
                    await SendNotificationAsync(message, baseUrl, sub);
            }
            else
            {
                _logger.LogInformation("Fetching SMS subscriber {Id} for sample", r.Id);
                var sub = await _db.SmsSubscribers.FirstOrDefaultAsync(s => s.Id == r.Id);
                if (sub != null)
                {
                    _logger.LogInformation("Sending sample SMS to {Phone}", sub.PhoneNumber);
                    await SendNotificationAsync(message, baseUrl, sub);
                }
            }
        }
    }

    private async Task SendToAllAsync(string message, string baseUrl, string type, string key)
    {
        var emails = await _db.Subscribers.Where(s => s.IsVerified).ToListAsync();
        foreach (var sub in emails)
            await SendNotificationAsync(message, baseUrl, sub);

        _logger.LogInformation("Retrieving all verified SMS subscribers");
        var phones = await _db.SmsSubscribers.Where(s => s.IsVerified).ToListAsync();
        _logger.LogInformation("Sending notification to {Count} SMS subscribers", phones.Count);
        foreach (var sub in phones)
            await SendNotificationAsync(message, baseUrl, sub);

        _db.SentNotifications.Add(new SentNotification
        {
            Type = type,
            Key = key,
            SentAt = _time.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
