namespace Predictorator.Options;

public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    public bool RunServer { get; set; } = true;
    public int QueuePollIntervalSeconds { get; set; } = 120;
    public int SchedulePollingIntervalSeconds { get; set; } = 120;
    public int ServerCheckIntervalSeconds { get; set; } = 120;
    public int HeartbeatIntervalSeconds { get; set; } = 120;
    public int WorkerCount { get; set; } = 1;
}
