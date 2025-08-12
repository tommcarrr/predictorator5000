using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Predictorator.Models;
using System.Linq;

namespace Predictorator.Services;

public class RecurringJobProcessor : BackgroundService
{
    private readonly TableClient _table;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeProvider _time;

    public RecurringJobProcessor(TableServiceClient client, IServiceScopeFactory scopeFactory, IDateTimeProvider time)
    {
        _table = client.GetTableClient("RecurringJobs");
        _table.CreateIfNotExists();
        _scopeFactory = scopeFactory;
        _time = time;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureJobAsync("CheckFixtures", "Daily", NextDailyUtc(1));
        await EnsureJobAsync("CountExpiredUnverified", "Weekly", NextWeeklyUtc(DayOfWeek.Monday, 1));
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var jobs = _table.Query<RecurringJob>().ToList();
        var tasks = jobs.Select(j => RunJobLoopAsync(j, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task RunJobLoopAsync(RecurringJob job, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var delay = job.NextRun - _time.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, token);

            using (var scope = _scopeFactory.CreateScope())
            {
                switch (job.RowKey)
                {
                    case "CheckFixtures":
                        var notif = scope.ServiceProvider.GetRequiredService<NotificationService>();
                        await notif.CheckFixturesAsync();
                        break;
                    case "CountExpiredUnverified":
                        var subs = scope.ServiceProvider.GetRequiredService<SubscriptionService>();
                        await subs.CountExpiredUnverifiedAsync();
                        break;
                }
            }

            job.NextRun = job.Interval == "Daily" ? job.NextRun.AddDays(1) : job.NextRun.AddDays(7);
            await _table.UpsertEntityAsync(job);
        }
    }

    private async Task EnsureJobAsync(string name, string interval, DateTimeOffset nextRun)
    {
        var existing = await _table.GetEntityIfExistsAsync<RecurringJob>("recurring", name);
        if (!existing.HasValue)
        {
            var job = new RecurringJob { RowKey = name, Interval = interval, NextRun = nextRun };
            await _table.AddEntityAsync(job);
        }
    }

    private DateTimeOffset NextDailyUtc(int hour)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(_time.UtcNow, tz);
        var next = nowLocal.Date.AddHours(hour);
        if (next <= nowLocal)
            next = next.AddDays(1);
        return TimeZoneInfo.ConvertTimeToUtc(next, tz);
    }

    private DateTimeOffset NextWeeklyUtc(DayOfWeek day, int hour)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(_time.UtcNow, tz);
        int daysUntil = ((int)day - (int)nowLocal.DayOfWeek + 7) % 7;
        var next = nowLocal.Date.AddDays(daysUntil).AddHours(hour);
        if (next <= nowLocal)
            next = next.AddDays(7);
        return TimeZoneInfo.ConvertTimeToUtc(next, tz);
    }
}

