using System.Collections.Concurrent;

namespace Predictorator.Services;

public class InMemoryRateLimitService : IRateLimitService
{
    private readonly int _maxRequests;
    private readonly TimeSpan _timeWindow;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ConcurrentDictionary<string, (int Count, DateTime Timestamp)> _requestCounts = new();

    public InMemoryRateLimitService(int maxRequests, TimeSpan timeWindow, IDateTimeProvider dateTimeProvider)
    {
        _maxRequests = maxRequests;
        _timeWindow = timeWindow;
        _dateTimeProvider = dateTimeProvider;
    }

    public bool ShouldLimit(string ipAddress)
    {
        var now = _dateTimeProvider.UtcNow;
        var entry = _requestCounts.AddOrUpdate(ipAddress, (1, now), (key, value) =>
        {
            if (now - value.Timestamp > _timeWindow)
            {
                return (1, now);
            }
            return (value.Count + 1, value.Timestamp);
        });
        return entry.Count > _maxRequests;
    }
}
