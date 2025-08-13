using Microsoft.Extensions.Logging;
using Predictorator.Core.Data;
using Predictorator.Core.Models;
using Resend;
using System.Linq;

namespace Predictorator.Core.Services;

public class NotificationService
{
    private static readonly TimeZoneInfo UkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
    private readonly IDataStore _store;
    private readonly IResend _resend;
    private readonly ITwilioSmsSender _sms;
    private readonly IConfiguration _config;
    private readonly IFixtureService _fixtures;
    private readonly IGameWeekService _gameWeeks;
    private readonly IDateRangeCalculator _range;
    private readonly NotificationFeatureService _features;
    private readonly IDateTimeProvider _time;
    private readonly IBackgroundJobService _jobs;
    private readonly EmailCssInliner _inliner;
    private readonly EmailTemplateRenderer _renderer;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IDataStore store,
        IResend resend,
        ITwilioSmsSender sms,
        IConfiguration config,
        IFixtureService fixtures,
        IGameWeekService gameWeeks,
        IDateRangeCalculator range,
        NotificationFeatureService features,
        IDateTimeProvider time,
        IBackgroundJobService jobs,
        EmailCssInliner inliner,
        EmailTemplateRenderer renderer,
        ILogger<NotificationService> logger)
    {
        _store = store;
        _resend = resend;
        _sms = sms;
        _config = config;
        _fixtures = fixtures;
        _gameWeeks = gameWeeks;
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

        var week = await _gameWeeks.GetNextGameWeekAsync(_time.Today);
        DateTime from;
        DateTime to;
        if (week != null)
        {
            from = week.StartDate;
            to = week.EndDate;
        }
        else
        {
            (from, to) = _range.GetDates(null, null, null);
        }

        var response = await _fixtures.GetFixturesAsync(from, to);
        if (response.Response.Count == 0)
            return;

        var nowUtc = _time.UtcNow;
        var nowUk = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, UkTimeZone);
        var ordered = response.Response.OrderBy(f => f.Fixture.Date).ToList();
        var future = ordered.FirstOrDefault(f => f.Fixture.Date.ToUniversalTime() > nowUtc);
        if (future != null)
        {
            var key = future.Fixture.Date.Date.ToString("yyyy-MM-dd");
            var sent = await _store.SentNotificationExistsAsync("NewFixtures", key);
            if (!sent)
            {
                var futureUk = TimeZoneInfo.ConvertTime(future.Fixture.Date, UkTimeZone);
                if (futureUk.Date == nowUk.Date)
                {
                    var sendTimeUk = futureUk.Date.AddHours(10);
                    var sendTimeUtc = TimeZoneInfo.ConvertTimeToUtc(sendTimeUk, UkTimeZone);
                    var delay = TimeExtensions.ClampDelay(sendTimeUtc, _time);
                    await _jobs.ScheduleAsync(
                        "SendNewFixturesAvailable",
                        new { Key = key, BaseUrl = baseUrl },
                        delay);
                }
            }
        }

        var first = ordered.First();
        var firstUk = TimeZoneInfo.ConvertTime(first.Fixture.Date, UkTimeZone);
        if (firstUk.Date == nowUk.Date)
        {
            var key = first.Fixture.Date.ToString("O");
            var sent = await _store.SentNotificationExistsAsync("FixturesStartingSoon", key);
            if (!sent)
            {
                var sendTimeUtc = first.Fixture.Date.AddHours(-2);
                var delay = TimeExtensions.ClampDelay(sendTimeUtc, _time);
                await _jobs.ScheduleAsync(
                    "SendFixturesStartingSoon",
                    new { Key = key, BaseUrl = baseUrl },
                    delay);
            }
        }
    }

    public async Task SendNewFixturesAvailableAsync(string key, string baseUrl)
    {
        await SendToAllAsync("Fixtures start today!", baseUrl, "NewFixtures", key);
    }

    public async Task SendFixturesStartingSoonAsync(string key, string baseUrl)
    {
        await SendToAllAsync("Fixtures start in 2 hours!", baseUrl, "FixturesStartingSoon", key);
    }

    private EmailMessage CreateEmailMessage(string message, string baseUrl, Subscriber sub)
    {
        var html = _renderer.Render(message, baseUrl, sub.UnsubscribeToken, "VIEW FIXTURES", baseUrl, preheader: message);
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
        var tasks = recipients.Select(async r =>
        {
            if (r.Type == "Email")
            {
                var sub = await _store.GetEmailSubscriberByIdAsync(r.Id);
                if (sub != null)
                    await SendNotificationAsync(message, baseUrl, sub);
            }
            else
            {
                _logger.LogInformation("Fetching SMS subscriber {Id} for sample", r.Id);
                var sub = await _store.GetSmsSubscriberByIdAsync(r.Id);
                if (sub != null)
                {
                    _logger.LogInformation("Sending sample SMS to {Phone}", sub.PhoneNumber);
                    await SendNotificationAsync(message, baseUrl, sub);
                }
            }
        }).ToList();

        await Task.WhenAll(tasks);
    }

    private async Task SendToAllAsync(string message, string baseUrl, string type, string key)
    {
        var emails = await _store.GetVerifiedEmailSubscribersAsync();
        var emailTasks = emails.Select(sub => SendNotificationAsync(message, baseUrl, sub));

        _logger.LogInformation("Retrieving all verified SMS subscribers");
        var phones = await _store.GetVerifiedSmsSubscribersAsync();
        _logger.LogInformation("Sending notification to {Count} SMS subscribers", phones.Count);
        var smsTasks = phones.Select(sub => SendNotificationAsync(message, baseUrl, sub));

        await Task.WhenAll(emailTasks.Concat(smsTasks));

        await _store.AddSentNotificationAsync(new SentNotification
        {
            Type = type,
            Key = key,
            SentAt = _time.UtcNow
        });
    }
}
