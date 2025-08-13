namespace Predictorator.Core.Services;

public interface ITwilioSmsSender
{
    Task SendSmsAsync(string to, string message);
}
