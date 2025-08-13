using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Predictorator.Services;

namespace Predictorator.Tests;

public class FixtureServiceDiTests
{
    [Fact]
    public void Service_provider_resolves_fixture_service()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddHybridCache();

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        services.AddSingleton(httpClientFactory);
        services.AddSingleton<IConfiguration>(Substitute.For<IConfiguration>());
        var env = Substitute.For<IHostEnvironment>();
        env.ContentRootPath.Returns(Directory.GetCurrentDirectory());
        services.AddSingleton(env);
        services.AddSingleton<CachePrefixService>();
        services.AddTransient<IFixtureService, FixtureService>();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IFixtureService>();

        Assert.NotNull(service);
    }
}
