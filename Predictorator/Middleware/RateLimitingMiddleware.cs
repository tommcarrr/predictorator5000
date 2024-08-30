using System.Collections.Concurrent;

namespace Predictorator.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _maxRequests;
    private readonly TimeSpan _timeWindow;
    private static readonly ConcurrentDictionary<string, (int Count, DateTime Timestamp)> _requestCounts = new();

    public RateLimitingMiddleware(RequestDelegate next, int maxRequests, TimeSpan timeWindow)
    {
        _next = next;
        _maxRequests = maxRequests;
        _timeWindow = timeWindow;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(ipAddress))
        {
            await _next(context);
            return;
        }

        var now = DateTime.UtcNow;

        _requestCounts.AddOrUpdate(ipAddress, (1, now), (key, value) =>
        {
            if (now - value.Timestamp > _timeWindow)
            {
                return (1, now);
            }
            else
            {
                return (value.Count + 1, value.Timestamp);
            }
        });

        if (_requestCounts[ipAddress].Count > _maxRequests)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        await _next(context);
    }
}