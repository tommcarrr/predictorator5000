using Azure;
using Azure.Data.Tables;

namespace Predictorator.Models;

public class BackgroundJob : ITableEntity
{
    public string PartitionKey { get; set; } = "jobs";
    public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
    public string JobType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset RunAt { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

