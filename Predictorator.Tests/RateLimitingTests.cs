using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Predictorator.Options;
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
                services.AddSingleton<IDateRangeCalculator, DateRangeCalculator>();
                services.AddSingleton<IDateTimeProvider>(new SystemDateTimeProvider());
                services.PostConfigure<RouteLimitingOptions>(o => o.UniqueRouteLimit = 1);
                services.AddTransient<IFixtureService>(_ => new FakeFixtureService(
                    new FixturesResponse { Response = new List<FixtureData>() }));

                var gwService = new FakeGameWeekService();
                gwService.Items.Add(new Predictorator.Models.GameWeek { Season = "24-25", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) });
                services.AddSingleton<IGameWeekService>(gwService);

                services.RemoveAll(typeof(IBrowserStorage));
                services.AddSingleton<IBrowserStorage>(new FakeBrowserStorage());
                services.AddScoped<UiModeService>();
            });
        });
    }

    [Fact(Skip="Requires table storage connection")]
    public async Task Returns_429_after_limit_exceeded()
    {
        var client = _factory.CreateClient();
        var first = await client.GetAsync("/");
        var second = await client.GetAsync("/other");

        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact(Skip="Requires table storage connection")]
    public async Task Excluded_ip_is_not_rate_limited()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<RouteLimitingOptions>(o => o.UniqueRouteLimit = 1);
                services.PostConfigure<RateLimitingOptions>(o => o.ExcludedIPs = new[] { "unknown" });
            });
        });

        var client = factory.CreateClient();
        var first = await client.GetAsync("/");
        var second = await client.GetAsync("/about");

        Assert.NotEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact(Skip="Requires table storage connection")]
    public async Task Excluded_forwarded_ip_is_not_rate_limited()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<RouteLimitingOptions>(o => o.UniqueRouteLimit = 1);
                services.PostConfigure<RateLimitingOptions>(o => o.ExcludedIPs = new[] { "1.2.3.4" });
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "1.2.3.4");
        var first = await client.GetAsync("/");
        var second = await client.GetAsync("/other");

        Assert.NotEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

}
