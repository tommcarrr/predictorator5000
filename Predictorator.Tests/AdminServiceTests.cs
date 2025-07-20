using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Services;
using Resend;
using Predictorator.Tests.Helpers;
using System.IO;
using System;

namespace Predictorator.Tests;

public class AdminServiceTests
{
    private static AdminService CreateService(out ApplicationDbContext db, out IResend resend, out ITwilioSmsSender sms)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new ApplicationDbContext(options);
        resend = Substitute.For<IResend>();
        sms = Substitute.For<ITwilioSmsSender>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resend:From"] = "from@example.com",
                ["BASE_URL"] = "http://localhost"
            })
            .Build();
        var env = new FakeWebHostEnvironment { WebRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) };
        Directory.CreateDirectory(Path.Combine(env.WebRootPath, "css"));
        File.WriteAllText(Path.Combine(env.WebRootPath, "css", "email.css"), "p{color:red;}");
        var inliner = new EmailCssInliner(env);
        return new AdminService(db, resend, sms, config, inliner);
    }

    [Fact]
    public async Task ConfirmAsync_marks_subscriber_verified()
    {
        var service = CreateService(out var db, out _, out _);
        var subscriber = new Subscriber { Email = "a", IsVerified = false, VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        db.Subscribers.Add(subscriber);
        await db.SaveChangesAsync();

        await service.ConfirmAsync("Email", subscriber.Id);

        Assert.True(subscriber.IsVerified);
    }

    [Fact]
    public async Task DeleteAsync_removes_sms_subscriber()
    {
        var service = CreateService(out var db, out _, out _);
        var smsSub = new SmsSubscriber { PhoneNumber = "+1", VerificationToken = "v", UnsubscribeToken = "u", CreatedAt = DateTime.UtcNow };
        db.SmsSubscribers.Add(smsSub);
        await db.SaveChangesAsync();

        await service.DeleteAsync("SMS", smsSub.Id);

        Assert.Empty(db.SmsSubscribers);
    }

    [Fact]
    public async Task SendNewFixturesSampleAsync_sends_email_and_sms()
    {
        var service = CreateService(out _, out var resend, out var sms);
        var recipients = new List<AdminSubscriberDto>
        {
            new(1, "user@example.com", true, "Email"),
            new(2, "+1", true, "SMS")
        };

        await service.SendNewFixturesSampleAsync(recipients);

        await resend.Received().EmailSendAsync(Arg.Is<EmailMessage>(m => m.HtmlBody != null && m.HtmlBody.Contains("style=")));
        await sms.Received().SendSmsAsync("+1", Arg.Any<string>());
    }

    [Fact]
    public async Task SendFixturesStartingSoonSampleAsync_sends_email_and_sms()
    {
        var service = CreateService(out _, out var resend, out var sms);
        var recipients = new List<AdminSubscriberDto>
        {
            new(1, "user@example.com", true, "Email"),
            new(2, "+1", true, "SMS")
        };

        await service.SendFixturesStartingSoonSampleAsync(recipients);

        await resend.Received().EmailSendAsync(Arg.Is<EmailMessage>(m => m.HtmlBody != null && m.HtmlBody.Contains("style=")));
        await sms.Received().SendSmsAsync("+1", Arg.Any<string>());
    }
}
