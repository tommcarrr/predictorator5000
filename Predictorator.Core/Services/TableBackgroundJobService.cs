using System.Text.Json;
using Azure.Data.Tables;
using Predictorator.Models;
using System.Linq;
using System.Collections.Generic;

namespace Predictorator.Services;

public class TableBackgroundJobService : IBackgroundJobService
{
    private readonly TableClient _table;
    private readonly IDateTimeProvider _time;

    public TableBackgroundJobService(TableServiceClient client, IDateTimeProvider time)
    {
        _table = client.GetTableClient("BackgroundJobs");
        _table.CreateIfNotExists();
        _time = time;
    }

    public Task ScheduleAsync(string jobType, object payload, TimeSpan delay)
    {
        var job = new BackgroundJob
        {
            JobType = jobType,
            Payload = JsonSerializer.Serialize(payload),
            RunAt = _time.UtcNow.Add(delay)
        };
        return _table.AddEntityAsync(job);
    }

    public Task<IReadOnlyList<BackgroundJob>> GetJobsAsync()
    {
        var jobs = _table.Query<BackgroundJob>().ToList();
        return Task.FromResult<IReadOnlyList<BackgroundJob>>(jobs);
    }

    public Task DeleteAsync(string id)
    {
        return _table.DeleteEntityAsync("jobs", id);
    }
}

