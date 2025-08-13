using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Predictorator.Core.Data;
using Predictorator.Core.Models;
using Predictorator.Core.Options;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;

namespace Predictorator.Core.Services;

public class GameWeekService : IGameWeekService
{
    private readonly IGameWeekRepository _repo;
    private readonly HybridCache _cache;
    private readonly CachePrefixService _prefix;
    private readonly HybridCacheEntryOptions _cacheOptions;

    public GameWeekService(
        IGameWeekRepository repo,
        HybridCache cache,
        CachePrefixService prefix,
        IOptions<GameWeekCacheOptions> options)
    {
        _repo = repo;
        _cache = cache;
        _prefix = prefix;
        var duration = TimeSpan.FromHours(options.Value.CacheDurationHours);
        _cacheOptions = new HybridCacheEntryOptions
        {
            Expiration = duration,
            LocalCacheExpiration = duration
        };
    }

    public Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
    {
        var cacheKey = $"{_prefix.Prefix}gameweeks_{season ?? "all"}";
        return GetOrCreateAsync<(IGameWeekRepository Repo, string? Season), List<GameWeek>>(cacheKey,
            (_repo, season),
            static (state, ct) => new ValueTask<List<GameWeek>>(state.Repo.GetGameWeeksAsync(state.Season)));
    }

    public Task<GameWeek?> GetGameWeekAsync(string season, int number)
    {
        var cacheKey = $"{_prefix.Prefix}gameweek_{season}_{number}";
        return GetOrCreateAsync<(IGameWeekRepository Repo, string Season, int Number), GameWeek?>(cacheKey,
            (_repo, season, number),
            static (state, ct) => new ValueTask<GameWeek?>(state.Repo.GetGameWeekAsync(state.Season, state.Number)));
    }

    public Task<GameWeek?> GetNextGameWeekAsync(DateTime date)
    {
        var cacheKey = $"{_prefix.Prefix}next_{date:yyyy-MM-dd}";
        return GetOrCreateAsync<(IGameWeekRepository Repo, DateTime Date), GameWeek?>(cacheKey,
            (_repo, date),
            static (state, ct) => new ValueTask<GameWeek?>(state.Repo.GetNextGameWeekAsync(state.Date)));
    }

    public async Task AddOrUpdateAsync(GameWeek gameWeek)
    {
        await _repo.AddOrUpdateAsync(gameWeek);
        await RemoveCacheEntriesAsync(
            $"{_prefix.Prefix}gameweeks_all",
            $"{_prefix.Prefix}gameweeks_{gameWeek.Season}",
            $"{_prefix.Prefix}gameweek_{gameWeek.Season}_{gameWeek.Number}",
            $"{_prefix.Prefix}next_{gameWeek.StartDate:yyyy-MM-dd}",
            $"{_prefix.Prefix}next_{gameWeek.EndDate:yyyy-MM-dd}");
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity != null)
        {
            await _repo.DeleteAsync(id);
            await RemoveCacheEntriesAsync(
                $"{_prefix.Prefix}gameweeks_all",
                $"{_prefix.Prefix}gameweeks_{entity.Season}",
                $"{_prefix.Prefix}gameweek_{entity.Season}_{entity.Number}",
                $"{_prefix.Prefix}next_{entity.StartDate:yyyy-MM-dd}",
                $"{_prefix.Prefix}next_{entity.EndDate:yyyy-MM-dd}");
        }
    }

    private Task RemoveCacheEntriesAsync(params string[] keys)
        => Task.WhenAll(keys.Select(k => _cache.RemoveAsync(k).AsTask()));

    private Task<TResult> GetOrCreateAsync<TState, TResult>(string key, TState state, Func<TState, CancellationToken, ValueTask<TResult>> factory)
    {
        return _cache.GetOrCreateAsync(key, state, factory, _cacheOptions).AsTask();
    }

    public async Task<string> ExportCsvAsync()
    {
        var items = await _repo.GetGameWeeksAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Season,Number,StartDate,EndDate");
        foreach (var g in items)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                g.Season,
                g.Number.ToString(CultureInfo.InvariantCulture),
                g.StartDate.ToString("O", CultureInfo.InvariantCulture),
                g.EndDate.ToString("O", CultureInfo.InvariantCulture)
            }));
        }
        return sb.ToString();
    }

    public async Task<int> ImportCsvAsync(Stream csv)
    {
        using var reader = new StreamReader(csv, Encoding.UTF8, leaveOpen: true);
        string? line;
        var added = 0;
        var first = true;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (first)
            {
                first = false;
                continue;
            }
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',', StringSplitOptions.None);
            if (parts.Length < 4) continue;

            var season = parts[0];
            if (!int.TryParse(parts[1], out var number)) continue;
            if (!DateTime.TryParse(parts[2], null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var start)) continue;
            if (!DateTime.TryParse(parts[3], null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var end)) continue;

            var existing = await _repo.GetGameWeekAsync(season, number);
            if (existing == null)
            {
                added++;
            }
            var gw = new GameWeek
            {
                Id = existing?.Id ?? 0,
                Season = season,
                Number = number,
                StartDate = start,
                EndDate = end
            };
            await AddOrUpdateAsync(gw);
        }
        return added;
    }
}
