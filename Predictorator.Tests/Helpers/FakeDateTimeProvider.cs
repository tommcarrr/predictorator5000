using Predictorator.Core.Services;

namespace Predictorator.Tests.Helpers;

public class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime Today { get; set; }
    public DateTime UtcNow { get; set; }
}
