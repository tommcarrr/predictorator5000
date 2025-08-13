namespace Predictorator.Core.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Today => DateTime.Today;
    public DateTime UtcNow => DateTime.UtcNow;
}
