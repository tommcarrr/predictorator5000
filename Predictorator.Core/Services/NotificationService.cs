using Microsoft.Extensions.Logging;
using Predictorator.Core.Data;
using Predictorator.Core.Models;
using System.Linq;

namespace Predictorator.Core.Services;

public class NotificationService
{
    private static readonly TimeZoneInfo UkTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
    private readonly IEmailSubscriberRepository _emails;
    private readonly ISmsSubscriberRepository _smsSubscribers;
    private readonly ISentNotificationRepository _sentNotifications;
    private readonly IConfiguration _config;
    private readonly IFixtureService _fixtures;
    private readonly IGameWeekService _gameWeeks;
    private readonly IDateRangeCalculator _range;
    private readonly NotificationFeatureService _features;
    private readonly IDateTimeProvider _time;
    private readonly IBackgroundJobService _jobs;
    private readonly INotificationSender<Subscriber> _emailSender;
    private readonly INotificationSender<SmsSubscriber> _smsSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailSubscriberRepository emails,
        ISmsSubscriberRepository smsSubscribers,
        ISentNotificationRepository sentNotifications,
        IConfiguration config,
        IFixtureService fixtures,
        IGameWeekService gameWeeks,
        IDateRangeCalculator range,
        NotificationFeatureService features,
        IDateTimeProvider time,
        IBackgroundJobService jobs,
        INotificationSender<Subscriber> emailSender,
        INotificationSender<SmsSubscriber> smsSender,
        ILogger<NotificationService> logger)
    {
        _emails = emails;
        _smsSubscribers = smsSubscribers;
        _sentNotifications = sentNotifications;
        _config = config;
        _fixtures = fixtures;
        _gameWeeks = gameWeeks;
        _range = range;
        _features = features;
        _time = time;
        _jobs = jobs;
        _emailSender = emailSender;
        _smsSender = smsSender;
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
            var sent = await _sentNotifications.SentNotificationExistsAsync("NewFixtures", key);
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
            var sent = await _sentNotifications.SentNotificationExistsAsync("FixturesStartingSoon", key);
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

    public async Task SendSampleAsync(IEnumerable<AdminSubscriberDto> recipients, string message, string baseUrl)
    {
        var tasks = recipients.Select(async r =>
        {
            if (r.Type == "Email")
            {
                var sub = await _emails.GetEmailSubscriberByIdAsync(r.Id);
                if (sub != null)
                    await _emailSender.SendAsync(message, baseUrl, sub);
            }
            else
            {
                _logger.LogInformation("Fetching SMS subscriber {Id} for sample", r.Id);
                var sub = await _smsSubscribers.GetSmsSubscriberByIdAsync(r.Id);
                if (sub != null)
                {
                    _logger.LogInformation("Sending sample SMS to {Phone}", sub.PhoneNumber);
                    await _smsSender.SendAsync(message, baseUrl, sub);
                }
            }
        }).ToList();

        await Task.WhenAll(tasks);
    }

    private async Task SendToAllAsync(string message, string baseUrl, string type, string key)
    {
        var emails = await _emails.GetVerifiedEmailSubscribersAsync();
        var emailTasks = emails.Select(sub => _emailSender.SendAsync(message, baseUrl, sub));

        _logger.LogInformation("Retrieving all verified SMS subscribers");
        var phones = await _smsSubscribers.GetVerifiedSmsSubscribersAsync();
        _logger.LogInformation("Sending notification to {Count} SMS subscribers", phones.Count);
        var smsTasks = phones.Select(sub => _smsSender.SendAsync(message, baseUrl, sub));

        await Task.WhenAll(emailTasks.Concat(smsTasks));

        await _sentNotifications.AddSentNotificationAsync(new SentNotification
        {
            Type = type,
            Key = key,
            SentAt = _time.UtcNow
        });
    }
}
