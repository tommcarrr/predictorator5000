using Microsoft.Extensions.Configuration;
using NSubstitute;
using Predictorator.Models;
using Predictorator.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Predictorator.Tests.Helpers;
using System.IO;
using Resend;

namespace Predictorator.Tests;

public class SubscriptionServiceTests
{
    private static SubscriptionService CreateService(out InMemoryDataStore store, out IResend resend, out ITwilioSmsSender sms,
        IDateTimeProvider? provider = null)
    {
        store = new InMemoryDataStore();
        resend = Substitute.For<IResend>();
        sms = Substitute.For<ITwilioSmsSender>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Resend:From"] = "from@example.com" })
            .Build();
        provider ??= new FakeDateTimeProvider { UtcNow = DateTime.UtcNow, Today = DateTime.Today };
        var env = new FakeWebHostEnvironment { WebRootPath = Path.GetTempPath() };
        var inliner = new EmailCssInliner(env);
        var renderer = new EmailTemplateRenderer();
        var logger = NullLogger<SubscriptionService>.Instance;
        return new SubscriptionService(store, resend, config, sms, provider, inliner, renderer, logger);
    }

    [Fact]
    public async Task SubscribeAsync_with_email_sends_email_with_links()
    {
        var service = CreateService(out var store, out var resend, out _);
        await service.SubscribeAsync("user@example.com", null, "http://localhost");

        await resend.Received().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        var subscriber = store.EmailSubscribers.Single();
        Assert.False(subscriber.IsVerified);
    }

    [Fact]
    public async Task VerifyAsync_marks_subscriber_verified()
    {
        var service = CreateService(out var store, out var resend, out _);
        var subscriber = new Subscriber { Email = "a", VerificationToken = "token", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        await store.AddEmailSubscriberAsync(subscriber);

        var result = await service.VerifyAsync("token");

        Assert.True(result);
        Assert.True(subscriber.IsVerified);
        await resend.Received().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnsubscribeAsync_removes_subscriber()
    {
        var service = CreateService(out var store, out _, out _);
        var subscriber = new Subscriber { Email = "a", VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        await store.AddEmailSubscriberAsync(subscriber);

        var result = await service.UnsubscribeAsync("u");

        Assert.True(result);
        Assert.Empty(store.EmailSubscribers);
    }

    [Fact]
    public async Task SubscribeAsync_with_phone_sends_sms()
    {
        var service = CreateService(out var store, out var resend, out var sms);
        await service.SubscribeAsync(null, "+123", "http://localhost");

        await sms.Received().SendSmsAsync("+123", Arg.Any<string>());
        await resend.Received().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        var subscriber = store.SmsSubscribers.Single();
        Assert.False(subscriber.IsVerified);
    }

    [Fact]
    public async Task SubscribeAsync_throws_when_both_email_and_phone_provided()
    {
        var service = CreateService(out _, out _, out _);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SubscribeAsync("e@example.com", "+1", "http://localhost"));
    }

    [Fact]
    public async Task VerifyAsync_marks_sms_subscriber_verified()
    {
        var service = CreateService(out var store, out var resend, out _);
        var subscriber = new SmsSubscriber { PhoneNumber = "+1", VerificationToken = "t", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        await store.AddSmsSubscriberAsync(subscriber);

        var result = await service.VerifyAsync("t");

        Assert.True(result);
        Assert.True(subscriber.IsVerified);
        await resend.Received().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnsubscribeAsync_removes_sms_subscriber()
    {
        var service = CreateService(out var store, out _, out _);
        var subscriber = new SmsSubscriber { PhoneNumber = "+1", VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        await store.AddSmsSubscriberAsync(subscriber);

        var result = await service.UnsubscribeAsync("u");

        Assert.True(result);
        Assert.Empty(store.SmsSubscribers);
    }

    [Fact]
    public async Task CountExpiredUnverifiedAsync_counts_old_records()
    {
        var provider = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow };
        var service = CreateService(out var store, out _, out _, provider);
        await store.AddEmailSubscriberAsync(new Subscriber { Email = "old@example.com", CreatedAt = provider.UtcNow.AddHours(-2), VerificationToken = "a", UnsubscribeToken = "b" });
        await store.AddSmsSubscriberAsync(new SmsSubscriber { PhoneNumber = "+1", CreatedAt = provider.UtcNow.AddHours(-2), VerificationToken = "c", UnsubscribeToken = "d" });

        var count = await service.CountExpiredUnverifiedAsync();

        Assert.Equal(2, count);
        Assert.Single(store.EmailSubscribers);
        Assert.Single(store.SmsSubscribers);
    }

    [Fact]
    public async Task CountExpiredUnverifiedAsync_excludes_recent_or_verified()
    {
        var provider = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow };
        var service = CreateService(out var store, out _, out _, provider);
        var recent = new Subscriber { Email = "new@example.com", CreatedAt = provider.UtcNow.AddMinutes(-30), VerificationToken = "a", UnsubscribeToken = "b" };
        var verified = new SmsSubscriber { PhoneNumber = "+1", CreatedAt = provider.UtcNow.AddHours(-2), IsVerified = true, VerificationToken = "c", UnsubscribeToken = "d" };
        await store.AddEmailSubscriberAsync(recent);
        await store.AddSmsSubscriberAsync(verified);

        var count = await service.CountExpiredUnverifiedAsync();

        Assert.Equal(0, count);
        Assert.Contains(recent, store.EmailSubscribers);
        Assert.Contains(verified, store.SmsSubscribers);
    }

    [Fact]
    public async Task UnsubscribeByContactAsync_email_case_insensitive()
    {
        var service = CreateService(out var store, out _, out _);
        await store.AddEmailSubscriberAsync(new Subscriber { Email = "User@Example.com", VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow });

        var result = await service.UnsubscribeByContactAsync("user@example.com");

        Assert.True(result);
        Assert.Empty(store.EmailSubscribers);
    }

    [Fact]
    public async Task UnsubscribeByContactAsync_matches_phone_formats()
    {
        var service = CreateService(out var store, out _, out _);
        await store.AddSmsSubscriberAsync(new SmsSubscriber { PhoneNumber = "5551234567", VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow });

        var result = await service.UnsubscribeByContactAsync("(555) 123-4567");

        Assert.True(result);
        Assert.Empty(store.SmsSubscribers);
    }
}
