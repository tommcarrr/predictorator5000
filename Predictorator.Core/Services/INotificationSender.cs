namespace Predictorator.Core.Services;

public interface INotificationSender<TSubscriber>
{
    Task SendAsync(string message, string baseUrl, TSubscriber subscriber);
}
