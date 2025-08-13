using Azure;
using Azure.Data.Tables;
using Predictorator.Core.Models;

namespace Predictorator.Core.Data;

public class TableGameWeekRepository : IGameWeekRepository
{
    private readonly TableClient _table;

    public TableGameWeekRepository(TableServiceClient client)
    {
        _table = client.GetTableClient("GameWeeks");
        _table.CreateIfNotExists();
    }

    private static GameWeek ToGameWeek(GameWeekEntity e) => new()
    {
        Id = e.Id,
        Season = e.PartitionKey,
        Number = int.Parse(e.RowKey),
        StartDate = e.StartDate,
        EndDate = e.EndDate
    };

    private static GameWeekEntity ToEntity(GameWeek g) => new()
    {
        PartitionKey = g.Season,
        RowKey = g.Number.ToString(),
        Id = g.Id,
        StartDate = g.StartDate,
        EndDate = g.EndDate
    };

    private async Task<int> GetNextIdAsync()
    {
        var max = 0;
        await foreach (var e in _table.QueryAsync<GameWeekEntity>(select: new[] { "Id" }))
        {
            if (e.Id > max) max = e.Id;
        }
        return max + 1;
    }

    public async Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
    {
        var list = new List<GameWeek>();
        var query = string.IsNullOrEmpty(season)
            ? _table.QueryAsync<GameWeekEntity>()
            : _table.QueryAsync<GameWeekEntity>(e => e.PartitionKey == season);
        await foreach (var e in query)
            list.Add(ToGameWeek(e));
        return list.OrderBy(g => g.Season).ThenBy(g => g.Number).ToList();
    }

    public async Task<GameWeek?> GetGameWeekAsync(string season, int number)
    {
        try
        {
            var e = await _table.GetEntityAsync<GameWeekEntity>(season, number.ToString());
            return ToGameWeek(e.Value);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<GameWeek?> GetByIdAsync(int id)
    {
        await foreach (var e in _table.QueryAsync<GameWeekEntity>(e => e.Id == id))
            return ToGameWeek(e);
        return null;
    }

    public async Task<GameWeek?> GetNextGameWeekAsync(DateTime date)
    {
        // Azure Table queries require DateTime values to be in UTC, otherwise the
        // filter will not match any entities. Ensure the supplied date is
        // explicitly marked as UTC before querying.
        var utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);

        var list = new List<GameWeek>();
        await foreach (var e in _table.QueryAsync<GameWeekEntity>(e => e.EndDate >= utcDate))
            list.Add(ToGameWeek(e));

        return list.OrderBy(g => g.StartDate).FirstOrDefault();
    }

    public async Task AddOrUpdateAsync(GameWeek gameWeek)
    {
        GameWeekEntity? existing = null;
        if (gameWeek.Id != 0)
        {
            await foreach (var e in _table.QueryAsync<GameWeekEntity>(e => e.Id == gameWeek.Id))
            {
                existing = e;
                break;
            }
        }
        if (existing == null)
        {
            try
            {
                var resp = await _table.GetEntityAsync<GameWeekEntity>(gameWeek.Season, gameWeek.Number.ToString());
                existing = resp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                existing = null;
            }
        }
        if (existing == null)
        {
            gameWeek.Id = await GetNextIdAsync();
            var entity = ToEntity(gameWeek);
            await _table.AddEntityAsync(entity);
        }
        else
        {
            var keyChanged = existing.PartitionKey != gameWeek.Season || existing.RowKey != gameWeek.Number.ToString();
            existing.PartitionKey = gameWeek.Season;
            existing.RowKey = gameWeek.Number.ToString();
            existing.StartDate = gameWeek.StartDate;
            existing.EndDate = gameWeek.EndDate;
            if (keyChanged)
            {
                await _table.DeleteEntityAsync(existing.PartitionKey, existing.RowKey);
                await _table.AddEntityAsync(existing);
            }
            else
            {
                await _table.UpsertEntityAsync(existing, TableUpdateMode.Replace);
            }
        }
    }

    public async Task DeleteAsync(int id)
    {
        GameWeekEntity? entity = null;
        await foreach (var e in _table.QueryAsync<GameWeekEntity>(e => e.Id == id))
        {
            entity = e;
            break;
        }
        if (entity != null)
        {
            await _table.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
        }
    }

    private class GameWeekEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
