using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Predictorator.Options;

namespace Predictorator.Middleware;

public class RouteLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RouteLimitingMiddleware> _logger;
    private readonly int _limit;
    private readonly HashSet<string> _excludedIps;

    public RouteLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IOptions<RouteLimitingOptions> options,
        IOptions<RateLimitingOptions> rateOptions,
        ILogger<RouteLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _limit = options.Value.UniqueRouteLimit;
        _excludedIps = rateOptions.Value.ExcludedIPs.ToHashSet();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (context.Request.Path.StartsWithSegments("/hangfire") ||
            _excludedIps.Contains(ip))
        {
            await _next(context);
            return;
        }

        var routes = _cache.GetOrCreate(ip, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        })!;

        var route = context.Request.Path.ToString().ToLowerInvariant();
        lock (routes)
        {
            if (!routes.Contains(route))
            {
                routes.Add(route);
                if (routes.Count > _limit)
                {
                    _logger.LogWarning("IP {Ip} exceeded unique route limit", ip);
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    return;
                }
            }
        }

        await _next(context);
    }
}
