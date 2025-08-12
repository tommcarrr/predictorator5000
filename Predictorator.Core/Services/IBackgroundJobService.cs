namespace Predictorator.Services;

public interface IBackgroundJobService
{
    Task ScheduleAsync(string jobType, object payload, TimeSpan delay);
}

