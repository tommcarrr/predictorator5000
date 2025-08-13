using Predictorator.Core.Models;

namespace Predictorator.Core.Data;

public interface ISentNotificationRepository
{
    Task<bool> SentNotificationExistsAsync(string type, string key);
    Task AddSentNotificationAsync(SentNotification notification);
}

