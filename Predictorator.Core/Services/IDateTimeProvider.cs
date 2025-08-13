namespace Predictorator.Core.Services;

public interface IDateTimeProvider
{
    DateTime Today { get; }
    DateTime UtcNow { get; }
}
