using Microsoft.AspNetCore.Http;
using NSubstitute;
using Predictorator.Middleware;
using Predictorator.Services;

namespace Predictorator.Tests;

public class RateLimitingMiddlewareTests
{
    [Fact]
    public async Task Returns_429_when_limit_exceeded()
    {
        var service = Substitute.For<IRateLimitService>();
        service.ShouldLimit(Arg.Any<string>(), Arg.Any<DateTime>()).Returns(true);
        var middleware = new RateLimitingMiddleware(_ => Task.CompletedTask, service);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invokes_next_when_not_limited()
    {
        var service = Substitute.For<IRateLimitService>();
        service.ShouldLimit(Arg.Any<string>(), Arg.Any<DateTime>()).Returns(false);
        var called = false;
        var middleware = new RateLimitingMiddleware(ctx => { called = true; return Task.CompletedTask; }, service);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.NotEqual(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }
}
