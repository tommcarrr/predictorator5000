using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class FixtureServiceTests
{
    [Fact]
    public async Task Fetches_from_http_and_caches_result()
    {
        var fixtures = new FixturesResponse { Response = [], FromDate = DateTime.Today, ToDate = DateTime.Today };
        var handler = new StubHttpMessageHandler(fixtures);
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        httpClientFactory.CreateClient("fixtures").Returns(client);
        var services = new ServiceCollection();
        services.AddHybridCache();
        var cache = services.BuildServiceProvider().GetRequiredService<HybridCache>();
        var prefix = new CachePrefixService();
        var service = new FixtureService(httpClientFactory, cache, prefix);

        var result1 = await service.GetFixturesAsync(DateTime.Today, DateTime.Today);
        var result2 = await service.GetFixturesAsync(DateTime.Today, DateTime.Today);

        Assert.Equal(1, handler.CallCount);
    }
}
