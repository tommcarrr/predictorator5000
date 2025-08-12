using Azure;
using Azure.Data.Tables;

namespace Predictorator.Models;

public class RecurringJob : ITableEntity
{
    public string PartitionKey { get; set; } = "recurring";
    public string RowKey { get; set; } = string.Empty;
    public string Interval { get; set; } = string.Empty; // "Daily" or "Weekly"
    public DateTimeOffset NextRun { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

