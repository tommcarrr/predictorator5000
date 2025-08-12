namespace Predictorator.Services;

using Predictorator.Models;

public interface IBackgroundJobService
{
    Task ScheduleAsync(string jobType, object payload, TimeSpan delay);

    Task<IReadOnlyList<BackgroundJob>> GetJobsAsync();

    Task DeleteAsync(string id);
}

