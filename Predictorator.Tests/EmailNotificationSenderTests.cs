using Microsoft.Extensions.Configuration;
using NSubstitute;
using Predictorator.Core.Models;
using Predictorator.Core.Services;
using Resend;
using System.Collections.Generic;

namespace Predictorator.Tests;

public class EmailNotificationSenderTests
{
    [Fact]
    public async Task SendAsync_sends_email_with_unsubscribe_and_styles()
    {
        var resend = Substitute.For<IResend>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Resend:From"] = "from@example.com" })
            .Build();
        var inliner = new EmailCssInliner();
        var renderer = new EmailTemplateRenderer();
        var sender = new EmailNotificationSender(resend, config, inliner, renderer);
        var sub = new Subscriber { Email = "u@example.com", UnsubscribeToken = "token" };

        await sender.SendAsync("hello", "http://localhost", sub);

        await resend.Received().EmailSendAsync(Arg.Is<EmailMessage>(m => m.HtmlBody != null && m.HtmlBody.Contains("Unsubscribe") && m.HtmlBody.Contains("style=")));
    }
}
