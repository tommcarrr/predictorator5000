using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;
using Resend;
using Hangfire;
using Microsoft.Extensions.Logging.Abstractions;
using Hangfire.Common;
using Hangfire.States;
using System.Linq.Expressions;
using System.IO;

namespace Predictorator.Tests;

public class NotificationServiceTests
{
    private static NotificationService CreateService(DateTime nowUtc, DateTime fixtureTimeUtc,
        out ApplicationDbContext db, out IBackgroundJobClient jobs,
        out IResend resend, out ITwilioSmsSender sms)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new ApplicationDbContext(options);
        jobs = Substitute.For<IBackgroundJobClient>();
        resend = Substitute.For<IResend>();
        sms = Substitute.For<ITwilioSmsSender>();

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
        var env = new FakeWebHostEnvironment { WebRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) };
        Directory.CreateDirectory(Path.Combine(env.WebRootPath, "css"));
        File.WriteAllText(Path.Combine(env.WebRootPath, "css", "email.css"), "p{color:red;}");
        var inliner = new EmailCssInliner(env);
        var renderer = new EmailTemplateRenderer();
        var gameWeeks = new FakeGameWeekService();
        gameWeeks.Items.Add(new GameWeek { Season = "25-26", Number = 1, StartDate = nowUtc.Date, EndDate = nowUtc.Date.AddDays(6) });
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance;
        return new NotificationService(db, resend, sms, config, fixtures, gameWeeks, calculator, features, provider, jobs, inliner, renderer, logger);
    }

    [Fact]
    public async Task CheckFixturesAsync_schedules_new_fixture_job()
    {
        var now = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var fixture = now.AddDays(2);
        var service = CreateService(now, fixture, out var db, out var jobs, out _, out _);

        await service.CheckFixturesAsync();

        jobs.Received().Create(
            Arg.Is<Job>(j => j.Method.Name == nameof(NotificationService.SendNewFixturesAvailableAsync)),
            Arg.Is<IState>(s => s is ScheduledState));
    }

    [Fact]
    public async Task SendNewFixturesAvailableAsync_sends_and_records()
    {
        var now = DateTime.UtcNow;
        var service = CreateService(now, now.AddDays(1), out var db, out _, out var resend, out var sms);
        db.Subscribers.Add(new Subscriber { Email = "u@example.com", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = now });
        db.SmsSubscribers.Add(new SmsSubscriber { PhoneNumber = "+1", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = now });
        await db.SaveChangesAsync();

        await service.SendNewFixturesAvailableAsync("key", "http://localhost");

        await resend.Received().EmailSendAsync(Arg.Is<EmailMessage>(m => m.HtmlBody != null && m.HtmlBody.Contains("Unsubscribe") && m.HtmlBody.Contains("style=")));
        await sms.Received().SendSmsAsync("+1", Arg.Is<string>(msg => msg.Contains("Unsubscribe:")));
        Assert.Equal(1, db.SentNotifications.Count());
    }
}
