using Azure;
using Azure.Data.Tables;
using Predictorator.Core.Models;

namespace Predictorator.Core.Data;

public class TableAnnouncementRepository : IAnnouncementRepository
{
    private readonly TableClient _table;
    private const string PartitionKey = "A";
    private const string RowKey = "settings";

    public TableAnnouncementRepository(TableServiceClient client)
    {
        _table = client.GetTableClient("Announcements");
        _table.CreateIfNotExists();
    }

    public async Task<Announcement?> GetAsync()
    {
        try
        {
            var entity = await _table.GetEntityAsync<AnnouncementEntity>(PartitionKey, RowKey);
            return ToModel(entity.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public Task UpsertAsync(Announcement announcement)
    {
        var entity = ToEntity(announcement);
        entity.PartitionKey = PartitionKey;
        entity.RowKey = RowKey;
        return _table.UpsertEntityAsync(entity);
    }

    private static Announcement ToModel(AnnouncementEntity e) => new()
    {
        Id = Guid.Parse(e.Id),
        Title = e.Title,
        Message = e.Message,
        IsEnabled = e.IsEnabled,
        ExpiresAt = e.ExpiresAt
    };

    private static AnnouncementEntity ToEntity(Announcement m) => new()
    {
        Id = m.Id.ToString(),
        Title = m.Title,
        Message = m.Message,
        IsEnabled = m.IsEnabled,
        ExpiresAt = m.ExpiresAt
    };

    private class AnnouncementEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
