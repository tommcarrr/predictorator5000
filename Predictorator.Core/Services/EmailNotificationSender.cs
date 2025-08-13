using Predictorator.Core.Models;
using Resend;

namespace Predictorator.Core.Services;

public class EmailNotificationSender : INotificationSender<Subscriber>
{
    private readonly IResend _resend;
    private readonly IConfiguration _config;
    private readonly EmailCssInliner _inliner;
    private readonly EmailTemplateRenderer _renderer;

    public EmailNotificationSender(
        IResend resend,
        IConfiguration config,
        EmailCssInliner inliner,
        EmailTemplateRenderer renderer)
    {
        _resend = resend;
        _config = config;
        _inliner = inliner;
        _renderer = renderer;
    }

    public async Task SendAsync(string message, string baseUrl, Subscriber subscriber)
    {
        var html = _renderer.Render(message, baseUrl, subscriber.UnsubscribeToken, "VIEW FIXTURES", baseUrl, preheader: message);
        var emailMessage = new EmailMessage
        {
            From = _config["Resend:From"] ?? "Prediction Fairy <no-reply@example.com>",
            Subject = "Predictorator Notification",
            HtmlBody = _inliner.InlineCss(html)
        };
        emailMessage.To.Add(subscriber.Email);
        await _resend.EmailSendAsync(emailMessage);
    }
}
