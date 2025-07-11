using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class FixtureServiceTestTokenTests
{
    [Fact]
    public async Task Returns_mock_data_when_token_matches()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(Directory.GetCurrentDirectory());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:TestToken"] = "token"
            })
            .Build();

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test-Token"] = "token";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        var handler = new StubHttpMessageHandler(new object());
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        httpClientFactory.CreateClient("fixtures").Returns(client);
        var services = new ServiceCollection();
        services.AddHybridCache();
        var cache = services.BuildServiceProvider().GetRequiredService<HybridCache>();

        var service = new FixtureService(httpClientFactory, cache, accessor, config, env);

        var result = await service.GetFixturesAsync(DateTime.Today, DateTime.Today.AddDays(6));

        Assert.Single(result.Response);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Fetches_from_http_when_token_does_not_match()
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.ContentRootPath.Returns(Directory.GetCurrentDirectory());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:TestToken"] = "token1"
            })
            .Build();

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test-Token"] = "token2";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);

        var fixtures = new FixturesResponse { Response = [], FromDate = DateTime.Today, ToDate = DateTime.Today };
        var handler = new StubHttpMessageHandler(fixtures);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        httpClientFactory.CreateClient("fixtures").Returns(client);
        var services = new ServiceCollection();
        services.AddHybridCache();
        var cache = services.BuildServiceProvider().GetRequiredService<HybridCache>();

        var service = new FixtureService(httpClientFactory, cache, accessor, config, env);

        var result = await service.GetFixturesAsync(DateTime.Today, DateTime.Today.AddDays(6));

        Assert.Empty(result.Response);
        Assert.Equal(1, handler.CallCount);
    }
}
