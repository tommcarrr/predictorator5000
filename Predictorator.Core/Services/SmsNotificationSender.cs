using Predictorator.Core.Models;

namespace Predictorator.Core.Services;

public class SmsNotificationSender : INotificationSender<SmsSubscriber>
{
    private readonly ITwilioSmsSender _sms;

    public SmsNotificationSender(ITwilioSmsSender sms)
    {
        _sms = sms;
    }

    public async Task SendAsync(string message, string baseUrl, SmsSubscriber subscriber)
    {
        var link = $"{baseUrl}/Subscription/Unsubscribe?token={subscriber.UnsubscribeToken}";
        var smsMessage = $"{message} {baseUrl}\n\n---\n\nUnsubscribe: {link}";
        await _sms.SendSmsAsync(subscriber.PhoneNumber, smsMessage);
    }
}
