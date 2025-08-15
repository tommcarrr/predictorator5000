using Azure;
using Azure.Data.Tables;

namespace Predictorator.Core.Models;

public class BackgroundJobError : ITableEntity
{
    public string PartitionKey { get; set; } = "errors";
    public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string StackTrace { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

