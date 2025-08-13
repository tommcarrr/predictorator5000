using Microsoft.Extensions.Configuration;
using NSubstitute;
using Predictorator.Core.Models;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;
using Predictorator.Core.Models.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System;
using System.Text;

namespace Predictorator.Tests;

public class AdminServiceTests
{
    private static AdminService CreateService(out InMemoryDataStore store, out INotificationSender<Subscriber> emailSender, out INotificationSender<SmsSubscriber> smsSender, out IBackgroundJobService jobs, out FakeDateTimeProvider provider)
    {
        store = new InMemoryDataStore();
        emailSender = Substitute.For<INotificationSender<Subscriber>>();
        smsSender = Substitute.For<INotificationSender<SmsSubscriber>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resend:From"] = "from@example.com",
                ["BASE_URL"] = "http://localhost"
            })
            .Build();
        provider = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow };
        var fixtures = new FakeFixtureService(new FixturesResponse());
        var range = new DateRangeCalculator(provider);
        var features = new NotificationFeatureService(config);
        jobs = Substitute.For<IBackgroundJobService>();
        var nLogger = NullLogger<NotificationService>.Instance;
        var gameWeeks = new FakeGameWeekService();
        gameWeeks.Items.Add(new GameWeek { Season = "25-26", Number = 1, StartDate = provider.UtcNow.Date, EndDate = provider.UtcNow.Date.AddDays(6) });
        var notifications = new NotificationService(store, store, store, config, fixtures, gameWeeks, range, features, provider, jobs, emailSender, smsSender, nLogger);
        var aLogger = NullLogger<AdminService>.Instance;
        var prefix = new CachePrefixService();
        return new AdminService(store, store, config, notifications, aLogger, jobs, provider, prefix, emailSender, smsSender);
    }

    [Fact]
    public async Task AddSubscriberAsync_adds_verified_email()
    {
        var service = CreateService(out var store, out _, out _, out _, out _);
        var dto = await service.AddSubscriberAsync("Email", "user@example.com");

        var sub = store.EmailSubscribers.Single();
        Assert.NotNull(dto);
        Assert.True(sub.IsVerified);
        Assert.Equal("user@example.com", sub.Email);
        Assert.Equal(sub.Id, dto!.Id);
    }

    [Fact]
    public async Task AddSubscriberAsync_adds_verified_sms()
    {
        var service = CreateService(out var store, out _, out _, out _, out _);
        var dto = await service.AddSubscriberAsync("SMS", "+1");

        var sub = store.SmsSubscribers.Single();
        Assert.NotNull(dto);
        Assert.True(sub.IsVerified);
        Assert.Equal("+1", sub.PhoneNumber);
        Assert.Equal(sub.Id, dto!.Id);
    }

    [Fact]
    public async Task ConfirmAsync_marks_subscriber_verified()
    {
        var service = CreateService(out var store, out _, out _, out _, out _);
        var subscriber = new Subscriber { Email = "a", IsVerified = false, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        await store.AddEmailSubscriberAsync(subscriber);

        await service.ConfirmAsync("Email", subscriber.Id);

        Assert.True(subscriber.IsVerified);
    }

    [Fact]
    public async Task DeleteAsync_removes_sms_subscriber()
    {
        var service = CreateService(out var store, out _, out _, out _, out _);
        var smsSub = new SmsSubscriber { PhoneNumber = "+1", VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        await store.AddSmsSubscriberAsync(smsSub);

        await service.DeleteAsync("SMS", smsSub.Id);

        Assert.Empty(store.SmsSubscribers);
    }

    [Fact]
    public async Task SendNewFixturesSampleAsync_sends_email_and_sms()
    {
        var service = CreateService(out var store, out var emailSender, out var smsSender, out _, out _);
        store.EmailSubscribers.Add(new Subscriber { Id = 1, Email = "user@example.com", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow });
        store.SmsSubscribers.Add(new SmsSubscriber { Id = 2, PhoneNumber = "+1", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow });
        var recipients = new List<AdminSubscriberDto>
        {
            new(1, "user@example.com", true, "Email"),
            new(2, "+1", true, "SMS")
        };

        await service.SendNewFixturesSampleAsync(recipients);

        await emailSender.Received().SendAsync("Fixtures start today!", Arg.Any<string>(), Arg.Any<Subscriber>());
        await smsSender.Received().SendAsync("Fixtures start today!", Arg.Any<string>(), Arg.Any<SmsSubscriber>());
    }

    [Fact]
    public async Task SendFixturesStartingSoonSampleAsync_sends_email_and_sms()
    {
        var service = CreateService(out var store, out var emailSender, out var smsSender, out _, out _);
        store.EmailSubscribers.Add(new Subscriber { Id = 1, Email = "user@example.com", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow });
        store.SmsSubscribers.Add(new SmsSubscriber { Id = 2, PhoneNumber = "+1", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow });
        var recipients = new List<AdminSubscriberDto>
        {
            new(1, "user@example.com", true, "Email"),
            new(2, "+1", true, "SMS")
        };

        await service.SendFixturesStartingSoonSampleAsync(recipients);

        await emailSender.Received().SendAsync("Fixtures start in 2 hours!", Arg.Any<string>(), Arg.Any<Subscriber>());
        await smsSender.Received().SendAsync("Fixtures start in 2 hours!", Arg.Any<string>(), Arg.Any<SmsSubscriber>());
    }

    [Fact]
    public async Task ScheduleFixturesStartingSoonSampleAsync_schedules_job()
    {
        var service = CreateService(out var store, out _, out _, out var jobs, out var time);
        var recipients = new List<AdminSubscriberDto>
        {
            new(1, "a@example.com", true, "Email")
        };

        var sendAt = time.UtcNow.AddMinutes(30);

        await service.ScheduleFixturesStartingSoonSampleAsync(recipients, sendAt);

        await jobs.Received().ScheduleAsync(
            "SendSample",
            Arg.Any<object>(),
            Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task ScheduleNewFixturesAsync_schedules_job()
    {
        var service = CreateService(out _, out _, out _, out var jobs, out var time);
        var sendAt = time.UtcNow.AddMinutes(30);

        await service.ScheduleNewFixturesAsync(sendAt);

        await jobs.Received().ScheduleAsync(
            "SendNewFixturesAvailable",
            Arg.Any<object>(),
            Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task ScheduleFixturesStartingSoonAsync_schedules_job()
    {
        var service = CreateService(out _, out _, out _, out var jobs, out var time);
        var sendAt = time.UtcNow.AddMinutes(30);

        await service.ScheduleFixturesStartingSoonAsync(sendAt);

        await jobs.Received().ScheduleAsync(
            "SendFixturesStartingSoon",
            Arg.Any<object>(),
            Arg.Any<TimeSpan>());
    }

    [Fact]
    public async Task ExportSubscribersCsvAsync_exports_all()
    {
        var service = CreateService(out var store, out _, out _, out _, out _);
        store.EmailSubscribers.Add(new Subscriber { Email = "user@example.com", IsVerified = true, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow });
        store.SmsSubscribers.Add(new SmsSubscriber { PhoneNumber = "+1", IsVerified = false, VerificationToken = "v2", UnsubscribeToken = "u2", CreatedAt = DateTime.UtcNow });

        var csv = await service.ExportSubscribersCsvAsync();
        var lines = csv.Trim().Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("Email,user@example.com", csv);
        Assert.Contains("SMS,+1", csv);
    }

    [Fact]
    public async Task ImportSubscribersCsvAsync_adds_new_and_skips_existing()
    {
        var service = CreateService(out var store, out _, out _, out _, out var provider);
        store.EmailSubscribers.Add(new Subscriber { Email = "existing@example.com", IsVerified = true, VerificationToken = "v1", UnsubscribeToken = "u1", CreatedAt = provider.UtcNow });
        var csv = "Type,Contact,IsVerified,VerificationToken,UnsubscribeToken,CreatedAt\n" +
                  $"Email,existing@example.com,true,v1,u1,{provider.UtcNow:O}\n" +
                  $"SMS,+1,true,v2,u2,{provider.UtcNow:O}\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var added = await service.ImportSubscribersCsvAsync(ms);

        Assert.Equal(1, added);
        Assert.Single(store.EmailSubscribers);
        Assert.Single(store.SmsSubscribers);
    }

    [Fact]
    public async Task GetJobsAsync_returns_jobs()
    {
        var service = CreateService(out _, out _, out _, out var jobs, out _);
        var list = new List<BackgroundJob> { new() { RowKey = "1", JobType = "Test", RunAt = DateTimeOffset.UtcNow } };
        jobs.GetJobsAsync().Returns(list);

        var result = await service.GetJobsAsync();

        Assert.Single(result);
        Assert.Equal("Test", result[0].JobType);
    }

    [Fact]
    public async Task DeleteJobAsync_calls_service()
    {
        var service = CreateService(out _, out _, out _, out var jobs, out _);

        await service.DeleteJobAsync("abc");

        await jobs.Received().DeleteAsync("abc");
    }
}
