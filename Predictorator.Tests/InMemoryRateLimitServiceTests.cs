using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class InMemoryRateLimitServiceTests
{
    [Fact]
    public void Limits_after_threshold_exceeded()
    {
        var service = new InMemoryRateLimitService(1, TimeSpan.FromMinutes(1));

        var now = DateTime.UtcNow;
        Assert.False(service.ShouldLimit("1.1.1.1", now));
        Assert.True(service.ShouldLimit("1.1.1.1", now));
    }

    [Fact]
    public void Resets_after_time_window()
    {
        var service = new InMemoryRateLimitService(1, TimeSpan.FromSeconds(1));
        var ip = "1.1.1.1";
        var now = DateTime.UtcNow;

        Assert.False(service.ShouldLimit(ip, now));
        Assert.True(service.ShouldLimit(ip, now));

        var later = now.AddSeconds(2);
        Assert.False(service.ShouldLimit(ip, later));
    }
}
