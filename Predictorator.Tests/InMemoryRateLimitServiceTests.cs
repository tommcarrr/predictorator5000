using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class InMemoryRateLimitServiceTests
{
    [Fact]
    public void Limits_after_threshold_exceeded()
    {
        var provider = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow };
        var service = new InMemoryRateLimitService(1, TimeSpan.FromMinutes(1), provider);

        Assert.False(service.ShouldLimit("1.1.1.1"));
        Assert.True(service.ShouldLimit("1.1.1.1"));
    }

    [Fact]
    public void Resets_after_time_window()
    {
        var provider = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow };
        var service = new InMemoryRateLimitService(1, TimeSpan.FromSeconds(1), provider);
        var ip = "1.1.1.1";

        Assert.False(service.ShouldLimit(ip));
        Assert.True(service.ShouldLimit(ip));
        provider.UtcNow = provider.UtcNow.AddSeconds(2);
        Assert.False(service.ShouldLimit(ip));
    }
}
