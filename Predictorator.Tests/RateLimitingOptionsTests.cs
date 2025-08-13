using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Predictorator.Core.Options;
using Predictorator.Startup;

namespace Predictorator.Tests;

public class RateLimitingOptionsTests
{
    [Fact]
    public void Disabled_by_default()
    {
        var options = new RateLimitingOptions();
        Assert.False(options.Enabled);
    }

    [Fact]
    public void Services_not_registered_when_disabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ApiSettings:RapidApiKey", "key"},
                {"Resend:ApiToken", "token"},
                {"TableStorage:ConnectionString", "UseDevelopmentStorage=true"},
                {"RateLimiting:Enabled", "false"}
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPredictoratorCore(config);

        Assert.DoesNotContain(services, s => s.ServiceType == typeof(IConfigureOptions<RateLimiterOptions>));
    }

    [Fact]
    public void Services_registered_when_enabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"ApiSettings:RapidApiKey", "key"},
                {"Resend:ApiToken", "token"},
                {"TableStorage:ConnectionString", "UseDevelopmentStorage=true"},
                {"RateLimiting:Enabled", "true"}
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPredictoratorCore(config);

        Assert.Contains(services, s => s.ServiceType == typeof(IConfigureOptions<RateLimiterOptions>));
    }
}

