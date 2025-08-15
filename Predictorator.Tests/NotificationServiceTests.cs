using Microsoft.Extensions.Configuration;
using NSubstitute;
using Predictorator.Core.Models;
using Predictorator.Core.Models.Fixtures;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Predictorator.Tests;

public class NotificationServiceTests
{
    private static NotificationService CreateService(DateTime nowUtc, DateTime fixtureTimeUtc,
        out InMemoryDataStore store, out IBackgroundJobService jobs,
        out INotificationSender<Subscriber> emailSender, out INotificationSender<SmsSubscriber> smsSender)
    {
        store = new InMemoryDataStore();
        jobs = Substitute.For<IBackgroundJobService>();
        emailSender = Substitute.For<INotificationSender<Subscriber>>();
        smsSender = Substitute.For<INotificationSender<SmsSubscriber>>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resend:ApiToken"] = "t",
                ["Resend:From"] = "from@example.com",
                ["Twilio:AccountSid"] = "sid",
                ["Twilio:AuthToken"] = "tok",
                ["Twilio:FromNumber"] = "+1",
                ["BASE_URL"] = "http://localhost"
            })
            .Build();
        var features = new NotificationFeatureService(config);
        var provider = new FakeDateTimeProvider { UtcNow = nowUtc, Today = nowUtc.Date };
        var calculator = new DateRangeCalculator(provider);
        var fixtures = new FakeFixtureService(new FixturesResponse
        {
            FromDate = nowUtc.Date,
            ToDate = nowUtc.Date.AddDays(6),
            Response = new List<FixtureData>
            {
                new()
                {
                    Fixture = new Fixture { Id = 1, Date = fixtureTimeUtc, Venue = new Venue { Name = "A", City = "B" } },
                    Teams = new Teams { Home = new Team { Name = "H" }, Away = new Team { Name = "A" } },
                    Score = new Score { Fulltime = new ScoreHomeAway() }
                }
            }
        });
        var inliner = new EmailCssInliner();
        var renderer = new EmailTemplateRenderer();
        var gameWeeks = new FakeGameWeekService();
        gameWeeks.Items.Add(new GameWeek { Season = "25-26", Number = 1, StartDate = nowUtc.Date, EndDate = nowUtc.Date.AddDays(6) });
        var logger = NullLogger<NotificationService>.Instance;
        // senders are substitutes so renderer/inliner unused but kept for completeness
        return new NotificationService(store, store, store, config, fixtures, gameWeeks, calculator, features, provider, jobs, emailSender, smsSender, logger);
    }

    [Fact]
    public async Task CheckFixturesAsync_schedules_new_fixture_job_on_fixture_day()
    {
        var now = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var fixture = now.AddHours(2);
        var service = CreateService(now, fixture, out _, out var jobs, out _, out _);

        await service.CheckFixturesAsync();

        await jobs.Received().ScheduleAsync(
            "SendNewFixturesAvailable",
            Arg.Any<object>(),
            Arg.Is<TimeSpan>(d => d == TimeSpan.FromMinutes(59)));
    }

    [Fact]
    public async Task CheckFixturesAsync_does_not_schedule_new_fixture_job_in_advance()
    {
        var now = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var fixture = now.AddDays(2);
        var service = CreateService(now, fixture, out _, out var jobs, out _, out _);

        await service.CheckFixturesAsync();

        await jobs.DidNotReceive().ScheduleAsync(
            "SendNewFixturesAvailable",
            Arg.Any<object>(),
            Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task SendNewFixturesAvailableAsync_sends_and_records()
    {
        var now = DateTime.UtcNow;
        var service = CreateService(now, now.AddDays(1), out var store, out _, out var emailSender, out var smsSender);
        store.EmailSubscribers.Add(new Subscriber { Email = "u@example.com", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = now });
        store.SmsSubscribers.Add(new SmsSubscriber { PhoneNumber = "+1", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = now });

        await service.SendNewFixturesAvailableAsync("key", "http://localhost");

        await emailSender.Received().SendAsync("Fixtures start today!", "http://localhost", Arg.Any<Subscriber>());
        await smsSender.Received().SendAsync("Fixtures start today!", "http://localhost", Arg.Any<SmsSubscriber>());
        Assert.Single(store.SentNotifications);
    }

    [Fact]
    public async Task SendNewFixturesAvailableAsync_handles_sender_errors()
    {
        var now = DateTime.UtcNow;
        var service = CreateService(now, now.AddDays(1), out var store, out _, out var emailSender, out _);
        var sub = new Subscriber { Email = "u@example.com", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = now };
        store.EmailSubscribers.Add(sub);
        emailSender
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), sub)
            .Returns<Task>(_ => throw new Exception("boom"));

        await service.SendNewFixturesAvailableAsync("key", "http://localhost");

        Assert.Single(store.SentNotifications);
    }
}
