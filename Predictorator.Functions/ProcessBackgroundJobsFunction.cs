using System.Text.Json;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Predictorator.Core.Models;
using Predictorator.Core.Options;
using Predictorator.Core.Services;
using Resend;

namespace Predictorator.Functions;

public class ProcessBackgroundJobsFunction
{
    private readonly TableClient _table;
    private readonly NotificationService _notifications;
    private readonly IDateTimeProvider _time;
    private readonly ILogger<ProcessBackgroundJobsFunction> _logger;
    private readonly IBackgroundJobErrorService _errors;
    private readonly IResend _resend;
    private readonly IConfiguration _config;
    private readonly EmailCssInliner _inliner;
    private readonly EmailTemplateRenderer _renderer;

    public ProcessBackgroundJobsFunction(
        TableServiceClient client,
        NotificationService notifications,
        IDateTimeProvider time,
        ILogger<ProcessBackgroundJobsFunction> logger,
        IBackgroundJobErrorService errors,
        IResend resend,
        IConfiguration config,
        EmailCssInliner inliner,
        EmailTemplateRenderer renderer)
    {
        _table = client.GetTableClient("BackgroundJobs");
        _table.CreateIfNotExists();
        _notifications = notifications;
        _time = time;
        _logger = logger;
        _errors = errors;
        _resend = resend;
        _config = config;
        _inliner = inliner;
        _renderer = renderer;
    }

    [Function("ProcessBackgroundJobs")]
    public async Task Run([TimerTrigger("%ProcessBackgroundJobsSchedule%")] TimerInfo timer)
    {
        var now = _time.UtcNow;
        _logger.LogInformation("Processing background jobs at {Time}", now);
        var jobs = _table.Query<BackgroundJob>(j => j.RunAt <= now).ToList();
        _logger.LogInformation("Found {Count} job(s) to process", jobs.Count);
        var results = new List<JobResult>();
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
                results.Add(new JobResult(job.RowKey, job.JobType, true, null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running background job {JobId}", job.RowKey);
                results.Add(new JobResult(job.RowKey, job.JobType, false, ex));
                await _errors.AddErrorAsync(new BackgroundJobError
                {
                    JobId = job.RowKey,
                    JobType = job.JobType,
                    Message = ex.Message,
                    StackTrace = ex.ToString(),
                    OccurredAt = _time.UtcNow
                });
            }
        }
        if (results.Any())
        {
            await SendReportAsync(results);
        }
        _logger.LogInformation("Background job processing completed at {Time}", _time.UtcNow);
    }

    private record SamplePayload(List<AdminSubscriberDto> Recipients, string Message, string BaseUrl);
    private record KeyPayload(string Key, string BaseUrl);

    private record JobResult(string JobId, string JobType, bool Success, Exception? Error);

    private async Task SendReportAsync(IEnumerable<JobResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Background job report:");
        foreach (var r in results)
        {
            if (r.Success)
            {
                sb.AppendLine($"{r.JobType} ({r.JobId}): Success");
            }
            else if (r.Error != null)
            {
                sb.AppendLine($"{r.JobType} ({r.JobId}): Failed - {r.Error.Message}");
                sb.AppendLine(r.Error.StackTrace);
            }
        }
        var body = sb.ToString();
        var html = _renderer.Render(body, _config["BASE_URL"] ?? string.Empty, null, preheader: body);
        var message = new EmailMessage
        {
            From = _config["Resend:From"] ?? "Prediction Fairy <no-reply@example.com>",
            Subject = "Background job report",
            HtmlBody = _inliner.InlineCss(html)
        };
        var admin = _config["ADMIN_EMAIL"] ?? _config[$"{AdminUserOptions.SectionName}:Email"] ?? "admin@example.com";
        message.To.Add(admin);
        await _resend.EmailSendAsync(message);
    }
}
