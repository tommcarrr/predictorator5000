using System.Text.Json;
using System.Linq;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Predictorator.Models;

namespace Predictorator.Services;

public class BackgroundJobProcessor : BackgroundService
{
    private readonly TableClient _table;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeProvider _time;
    private readonly ILogger<BackgroundJobProcessor> _logger;

    public BackgroundJobProcessor(TableServiceClient client, IServiceScopeFactory scopeFactory, IDateTimeProvider time, ILogger<BackgroundJobProcessor> logger)
    {
        _table = client.GetTableClient("BackgroundJobs");
        _table.CreateIfNotExists();
        _scopeFactory = scopeFactory;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = _time.UtcNow;
            var jobs = _table.Query<BackgroundJob>(j => j.RunAt <= now).ToList();
            foreach (var job in jobs)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();
                    switch (job.JobType)
                    {
                        case "SendSample":
                            var sample = JsonSerializer.Deserialize<SamplePayload>(job.Payload)!;
                            await notifications.SendSampleAsync(sample.Recipients, sample.Message, sample.BaseUrl);
                            break;
                        case "SendNewFixturesAvailable":
                            var nf = JsonSerializer.Deserialize<KeyPayload>(job.Payload)!;
                            await notifications.SendNewFixturesAvailableAsync(nf.Key, nf.BaseUrl);
                            break;
                        case "SendFixturesStartingSoon":
                            var fs = JsonSerializer.Deserialize<KeyPayload>(job.Payload)!;
                            await notifications.SendFixturesStartingSoonAsync(fs.Key, fs.BaseUrl);
                            break;
                    }
                    await _table.DeleteEntityAsync(job.PartitionKey, job.RowKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running background job {JobId}", job.RowKey);
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private record SamplePayload(List<AdminSubscriberDto> Recipients, string Message, string BaseUrl);
    private record KeyPayload(string Key, string BaseUrl);
}

