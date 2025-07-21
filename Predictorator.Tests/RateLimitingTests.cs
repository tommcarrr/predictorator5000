using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Predictorator.Data;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
                services.AddSingleton<IDateRangeCalculator, DateRangeCalculator>();
                services.AddSingleton<IDateTimeProvider>(new SystemDateTimeProvider());
                services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 1,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                    });
                });
                services.AddTransient<IFixtureService>(_ => new FakeFixtureService(
                    new FixturesResponse { Response = new List<FixtureData>() }));

                services.RemoveAll(typeof(IBrowserStorage));
                services.AddSingleton<IBrowserStorage>(new FakeBrowserStorage());
                services.AddScoped<UiModeService>();
            });
        });
    }

    [Fact]
    public async Task Returns_429_after_limit_exceeded()
    {
        var client = _factory.CreateClient();
        var first = await client.GetAsync("/");
        var second = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task Excluded_ip_is_not_rate_limited()
    {
        string? observedIp = null;
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDbExempt"));
                services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                    var excluded = new HashSet<string> { "unknown" };
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        observedIp = ip;
                        if (excluded.Contains(ip))
                            return RateLimitPartition.GetNoLimiter(ip);
                        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 1,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                    });
                });
            });
        });

        var client = factory.CreateClient();
        var first = await client.GetAsync("/");
        var second = await client.GetAsync("/");

        Assert.Equal("unknown", observedIp);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }
}
