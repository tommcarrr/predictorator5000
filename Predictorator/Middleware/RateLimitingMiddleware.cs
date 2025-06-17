using Predictorator.Services;

namespace Predictorator.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitService _rateLimitService;

    public RateLimitingMiddleware(RequestDelegate next, IRateLimitService rateLimitService)
    {
        _next = next;
        _rateLimitService = rateLimitService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(ipAddress))
        {
            await _next(context);
            return;
        }

        if (_rateLimitService.ShouldLimit(ipAddress))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            return;
        }

        await _next(context);
    }
}