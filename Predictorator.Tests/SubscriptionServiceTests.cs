using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Resend;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Services;
using Xunit;

namespace Predictorator.Tests;

public class SubscriptionServiceTests
{
    private static SubscriptionService CreateService(out ApplicationDbContext db, out IResend resend)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new ApplicationDbContext(options);
        resend = Substitute.For<IResend>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Resend:From"] = "from@example.com" })
            .Build();
        return new SubscriptionService(db, resend, config);
    }

    [Fact]
    public async Task AddSubscriberAsync_sends_email_with_links()
    {
        var service = CreateService(out var db, out var resend);
        await service.AddSubscriberAsync("user@example.com", "http://localhost");

        await resend.Received().EmailSendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        var subscriber = await db.Subscribers.SingleAsync();
        Assert.False(subscriber.IsVerified);
    }

    [Fact]
    public async Task VerifyAsync_marks_subscriber_verified()
    {
        var service = CreateService(out var db, out _);
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
        var service = CreateService(out var db, out _);
        var subscriber = new Subscriber { Email = "a", VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        db.Subscribers.Add(subscriber);
        await db.SaveChangesAsync();

        var result = await service.UnsubscribeAsync("u");

        Assert.True(result);
        Assert.Empty(db.Subscribers);
    }
}
