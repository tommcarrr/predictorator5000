using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Options;
using System.Globalization;
using System.IO;
using System.Text;

namespace Predictorator.Services;

public class GameWeekService : IGameWeekService
{
    private readonly IGameWeekRepository _repo;
    private readonly HybridCache _cache;
    private readonly CachePrefixService _prefix;
    private readonly TimeSpan _cacheDuration;

    public GameWeekService(
        IGameWeekRepository repo,
        HybridCache cache,
        CachePrefixService prefix,
        IOptions<GameWeekCacheOptions> options)
    {
        _repo = repo;
        _cache = cache;
        _prefix = prefix;
        _cacheDuration = TimeSpan.FromHours(options.Value.CacheDurationHours);
    }

    public Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
    {
        var cacheKey = $"{_prefix.Prefix}gameweeks_{season ?? "all"}";
        var options = new HybridCacheEntryOptions
        {
            Expiration = _cacheDuration,
            LocalCacheExpiration = _cacheDuration
        };

        return _cache.GetOrCreateAsync<(IGameWeekRepository Repo, string? Season), List<GameWeek>>(cacheKey,
            (_repo, season),
            static (state, ct) => new ValueTask<List<GameWeek>>(state.Repo.GetGameWeeksAsync(state.Season)),
            options).AsTask();
    }

    public Task<GameWeek?> GetGameWeekAsync(string season, int number)
    {
        var cacheKey = $"{_prefix.Prefix}gameweek_{season}_{number}";
        var options = new HybridCacheEntryOptions
        {
            Expiration = _cacheDuration,
            LocalCacheExpiration = _cacheDuration
        };

        return _cache.GetOrCreateAsync<(IGameWeekRepository Repo, string Season, int Number), GameWeek?>(cacheKey,
            (_repo, season, number),
            static (state, ct) => new ValueTask<GameWeek?>(state.Repo.GetGameWeekAsync(state.Season, state.Number)),
            options).AsTask();
    }

    public Task<GameWeek?> GetNextGameWeekAsync(DateTime date)
    {
        var cacheKey = $"{_prefix.Prefix}next_{date:yyyy-MM-dd}";
        var options = new HybridCacheEntryOptions
        {
            Expiration = _cacheDuration,
            LocalCacheExpiration = _cacheDuration
        };

        return _cache.GetOrCreateAsync<(IGameWeekRepository Repo, DateTime Date), GameWeek?>(cacheKey,
            (_repo, date),
            static (state, ct) => new ValueTask<GameWeek?>(state.Repo.GetNextGameWeekAsync(state.Date)),
            options).AsTask();
    }

    public async Task AddOrUpdateAsync(GameWeek gameWeek)
    {
        await _repo.AddOrUpdateAsync(gameWeek);
        await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_all");
        await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_{gameWeek.Season}");
        await _cache.RemoveAsync($"{_prefix.Prefix}gameweek_{gameWeek.Season}_{gameWeek.Number}");
        await _cache.RemoveAsync($"{_prefix.Prefix}next_{gameWeek.StartDate:yyyy-MM-dd}");
        await _cache.RemoveAsync($"{_prefix.Prefix}next_{gameWeek.EndDate:yyyy-MM-dd}");
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity != null)
        {
            await _repo.DeleteAsync(id);
            await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_all");
            await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_{entity.Season}");
            await _cache.RemoveAsync($"{_prefix.Prefix}gameweek_{entity.Season}_{entity.Number}");
            await _cache.RemoveAsync($"{_prefix.Prefix}next_{entity.StartDate:yyyy-MM-dd}");
            await _cache.RemoveAsync($"{_prefix.Prefix}next_{entity.EndDate:yyyy-MM-dd}");
        }
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
