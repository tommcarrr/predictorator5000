using NSubstitute;
using Predictorator.Core.Models;
using Predictorator.Core.Services;

namespace Predictorator.Tests;

public class SmsNotificationSenderTests
{
    [Fact]
    public async Task SendAsync_sends_message_with_unsubscribe_link()
    {
        var sms = Substitute.For<ITwilioSmsSender>();
        var sender = new SmsNotificationSender(sms);
        var sub = new SmsSubscriber { PhoneNumber = "+1", UnsubscribeToken = "u" };

        await sender.SendAsync("msg", "http://localhost", sub);

        await sms.Received().SendSmsAsync("+1", Arg.Is<string>(m => m.Contains("Unsubscribe:")));
    }
}
