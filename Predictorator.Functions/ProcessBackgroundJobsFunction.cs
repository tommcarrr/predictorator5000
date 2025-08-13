using System.Text.Json;
using System.Linq;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;
using Predictorator.Core.Models;
using Predictorator.Core.Services;

namespace Predictorator.Functions;

public class ProcessBackgroundJobsFunction
{
    private readonly TableClient _table;
    private readonly NotificationService _notifications;
    private readonly IDateTimeProvider _time;
    private readonly ILogger<ProcessBackgroundJobsFunction> _logger;

    public ProcessBackgroundJobsFunction(TableServiceClient client, NotificationService notifications, IDateTimeProvider time, ILogger<ProcessBackgroundJobsFunction> logger)
    {
        _table = client.GetTableClient("BackgroundJobs");
        _table.CreateIfNotExists();
        _notifications = notifications;
        _time = time;
        _logger = logger;
    }

    [Function("ProcessBackgroundJobs")]
    public async Task Run([TimerTrigger("%ProcessBackgroundJobsSchedule%")] TimerInfo timer)
    {
        var now = _time.UtcNow;
        _logger.LogInformation("Processing background jobs at {Time}", now);
        var jobs = _table.Query<BackgroundJob>(j => j.RunAt <= now).ToList();
        _logger.LogInformation("Found {Count} job(s) to process", jobs.Count);
        foreach (var job in jobs)
        {
            try
            {
                _logger.LogInformation("Running job {JobId} of type {JobType}", job.RowKey, job.JobType);
                switch (job.JobType)
                {
                    case "SendSample":
                        var sample = JsonSerializer.Deserialize<SamplePayload>(job.Payload)!;
                        await _notifications.SendSampleAsync(sample.Recipients, sample.Message, sample.BaseUrl);
                        break;
                    case "SendNewFixturesAvailable":
                        var nf = JsonSerializer.Deserialize<KeyPayload>(job.Payload)!;
                        await _notifications.SendNewFixturesAvailableAsync(nf.Key, nf.BaseUrl);
                        break;
                    case "SendFixturesStartingSoon":
                        var fs = JsonSerializer.Deserialize<KeyPayload>(job.Payload)!;
                        await _notifications.SendFixturesStartingSoonAsync(fs.Key, fs.BaseUrl);
                        break;
                }
                await _table.DeleteEntityAsync(job.PartitionKey, job.RowKey);
                _logger.LogInformation("Job {JobId} processed", job.RowKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running background job {JobId}", job.RowKey);
            }
        }
        _logger.LogInformation("Background job processing completed at {Time}", _time.UtcNow);
    }

    private record SamplePayload(List<AdminSubscriberDto> Recipients, string Message, string BaseUrl);
    private record KeyPayload(string Key, string BaseUrl);
}
