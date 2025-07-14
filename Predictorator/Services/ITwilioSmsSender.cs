namespace Predictorator.Services;

public interface ITwilioSmsSender
{
    Task SendSmsAsync(string to, string message);
}
