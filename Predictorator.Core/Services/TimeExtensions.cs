using System;

namespace Predictorator.Core.Services;

public static class TimeExtensions
{
    public static TimeSpan ClampDelay(DateTime target, IDateTimeProvider now)
    {
        var delay = target - now.UtcNow;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }
}

