namespace Predictorator.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string to, string link);
    Task SendUnsubscribeEmailAsync(string to, string link);
}
