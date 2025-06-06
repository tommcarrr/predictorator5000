using System.Collections.Concurrent;

namespace Predictorator.Services;

public class InMemoryRateLimitService : IRateLimitService
{
    private readonly int _maxRequests;
    private readonly TimeSpan _timeWindow;
    private readonly ConcurrentDictionary<string, (int Count, DateTime Timestamp)> _requestCounts = new();

    public InMemoryRateLimitService(int maxRequests, TimeSpan timeWindow)
    {
        _maxRequests = maxRequests;
        _timeWindow = timeWindow;
    }

    public bool ShouldLimit(string ipAddress, DateTime now)
    {
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
