using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Predictorator.Middleware;

namespace Predictorator.Tests;

public class MaintenanceMiddlewareTests
{
    [Fact]
    public async Task Returns_503_when_enabled()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "maintenance.html"), "test");
        var env = Substitute.For<IWebHostEnvironment>();
        env.WebRootPath.Returns(tempDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Maintenance:Enabled"] = "true" })
            .Build();

        var middleware = new MaintenanceMiddleware(_ => Task.CompletedTask, config, env);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task Invokes_next_when_disabled()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        var config = new ConfigurationBuilder().Build();
        var called = false;
        var middleware = new MaintenanceMiddleware(_ => { called = true; return Task.CompletedTask; }, config, env);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(called);
    }
}
