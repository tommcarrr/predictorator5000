namespace Predictorator.Services;

public interface IDateTimeProvider
{
    DateTime Today { get; }
    DateTime UtcNow { get; }
}
