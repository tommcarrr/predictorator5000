using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class TimeExtensionsTests
{
    [Fact]
    public void ClampDelay_FutureDate_ReturnsDifference()
    {
        var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var provider = new FakeDateTimeProvider { UtcNow = now };
        var target = now.AddMinutes(5);

        var delay = TimeExtensions.ClampDelay(target, provider);

        Assert.Equal(TimeSpan.FromMinutes(5), delay);
    }

    [Fact]
    public void ClampDelay_PastDate_ReturnsZero()
    {
        var now = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var provider = new FakeDateTimeProvider { UtcNow = now };
        var target = now.AddMinutes(-5);

        var delay = TimeExtensions.ClampDelay(target, provider);

        Assert.Equal(TimeSpan.Zero, delay);
    }
}

