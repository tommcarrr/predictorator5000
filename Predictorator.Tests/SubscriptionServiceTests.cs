using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Services;
using Resend;

namespace Predictorator.Tests;

public class SubscriptionServiceTests
{
    private static SubscriptionService CreateService(out ApplicationDbContext db, out IResend resend, out ITwilioSmsSender sms)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new ApplicationDbContext(options);
        resend = Substitute.For<IResend>();
        sms = Substitute.For<ITwilioSmsSender>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Resend:From"] = "from@example.com" })
            .Build();
        return new SubscriptionService(db, resend, config, sms);
    }

    [Fact]
    public async Task AddSubscriberAsync_sends_email_with_links()
    {
        var service = CreateService(out var db, out var resend, out _);
        await service.AddSubscriberAsync("user@example.com", "http://localhost");

        await resend.Received().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        var subscriber = await db.Subscribers.SingleAsync();
        Assert.False(subscriber.IsVerified);
    }

    [Fact]
    public async Task VerifyAsync_marks_subscriber_verified()
    {
        var service = CreateService(out var db, out _, out _);
        var subscriber = new Subscriber { Email = "a", VerificationToken = "token", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        db.Subscribers.Add(subscriber);
        await db.SaveChangesAsync();

        var result = await service.VerifyAsync("token");

        Assert.True(result);
        Assert.True(subscriber.IsVerified);
    }

    [Fact]
    public async Task UnsubscribeAsync_removes_subscriber()
    {
        var service = CreateService(out var db, out _, out _);
        var subscriber = new Subscriber { Email = "a", VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        db.Subscribers.Add(subscriber);
        await db.SaveChangesAsync();

        var result = await service.UnsubscribeAsync("u");

        Assert.True(result);
        Assert.Empty(db.Subscribers);
    }

    [Fact]
    public async Task AddSmsSubscriberAsync_sends_sms()
    {
        var service = CreateService(out var db, out _, out var sms);
        await service.AddSmsSubscriberAsync("+123", "http://localhost");

        await sms.Received().SendSmsAsync("+123", Arg.Any<string>());
        var subscriber = await db.SmsSubscribers.SingleAsync();
        Assert.False(subscriber.IsVerified);
    }

    [Fact]
    public async Task VerifyAsync_marks_sms_subscriber_verified()
    {
        var service = CreateService(out var db, out _, out _);
        var subscriber = new SmsSubscriber { PhoneNumber = "+1", VerificationToken = "t", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        db.SmsSubscribers.Add(subscriber);
        await db.SaveChangesAsync();

        var result = await service.VerifyAsync("t");

        Assert.True(result);
        Assert.True(subscriber.IsVerified);
    }

    [Fact]
    public async Task UnsubscribeAsync_removes_sms_subscriber()
    {
        var service = CreateService(out var db, out _, out _);
        var subscriber = new SmsSubscriber { PhoneNumber = "+1", VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        db.SmsSubscribers.Add(subscriber);
        await db.SaveChangesAsync();

        var result = await service.UnsubscribeAsync("u");

        Assert.True(result);
        Assert.Empty(db.SmsSubscribers);
    }
}
