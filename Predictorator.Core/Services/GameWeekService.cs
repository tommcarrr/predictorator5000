using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Options;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Predictorator.Services;

public class GameWeekService : IGameWeekService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly HybridCache _cache;
    private readonly CachePrefixService _prefix;
    private readonly TimeSpan _cacheDuration;

    public GameWeekService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        HybridCache cache,
        CachePrefixService prefix,
        IOptions<GameWeekCacheOptions> options)
    {
        _dbFactory = dbFactory;
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

        return _cache.GetOrCreateAsync(cacheKey, async ct =>
        {
            using var db = _dbFactory.CreateDbContext();
            var query = db.GameWeeks.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(season))
                query = query.Where(g => g.Season == season);
            return await query.OrderBy(g => g.Season).ThenBy(g => g.Number).ToListAsync(ct);
        }, options).AsTask();
    }

    public Task<GameWeek?> GetGameWeekAsync(string season, int number)
    {
        var cacheKey = $"{_prefix.Prefix}gameweek_{season}_{number}";
        var options = new HybridCacheEntryOptions
        {
            Expiration = _cacheDuration,
            LocalCacheExpiration = _cacheDuration
        };

        return _cache.GetOrCreateAsync(cacheKey, async ct =>
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.GameWeeks
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Season == season && g.Number == number, ct);
        }, options).AsTask();
    }

    public Task<GameWeek?> GetNextGameWeekAsync(DateTime date)
    {
        var cacheKey = $"{_prefix.Prefix}next_{date:yyyy-MM-dd}";
        var options = new HybridCacheEntryOptions
        {
            Expiration = _cacheDuration,
            LocalCacheExpiration = _cacheDuration
        };

        return _cache.GetOrCreateAsync(cacheKey, async ct =>
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.GameWeeks
                .Where(g => g.EndDate >= date)
                .OrderBy(g => g.StartDate)
                .FirstOrDefaultAsync(ct);
        }, options).AsTask();
    }

    public async Task AddOrUpdateAsync(GameWeek gameWeek)
    {
        await using var db = _dbFactory.CreateDbContext();
        GameWeek? existing = null;

        if (gameWeek.Id != 0)
        {
            existing = await db.GameWeeks.FindAsync(gameWeek.Id);
        }

        if (existing == null)
        {
            existing = await db.GameWeeks
                .FirstOrDefaultAsync(g => g.Season == gameWeek.Season && g.Number == gameWeek.Number);
        }

        if (existing == null)
        {
            db.GameWeeks.Add(gameWeek);
        }
        else
        {
            existing.Season = gameWeek.Season;
            existing.Number = gameWeek.Number;
            existing.StartDate = gameWeek.StartDate;
            existing.EndDate = gameWeek.EndDate;
        }

        await db.SaveChangesAsync();

        await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_all");
        await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_{gameWeek.Season}");
        await _cache.RemoveAsync($"{_prefix.Prefix}gameweek_{gameWeek.Season}_{gameWeek.Number}");
        await _cache.RemoveAsync($"{_prefix.Prefix}next_{gameWeek.StartDate:yyyy-MM-dd}");
        await _cache.RemoveAsync($"{_prefix.Prefix}next_{gameWeek.EndDate:yyyy-MM-dd}");
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = _dbFactory.CreateDbContext();
        var entity = await db.GameWeeks.FindAsync(id);
        if (entity != null)
        {
            db.GameWeeks.Remove(entity);
            await db.SaveChangesAsync();
            await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_all");
            await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_{entity.Season}");
            await _cache.RemoveAsync($"{_prefix.Prefix}gameweek_{entity.Season}_{entity.Number}");
            await _cache.RemoveAsync($"{_prefix.Prefix}next_{entity.StartDate:yyyy-MM-dd}");
            await _cache.RemoveAsync($"{_prefix.Prefix}next_{entity.EndDate:yyyy-MM-dd}");
        }
    }

    public async Task<string> ExportCsvAsync()
    {
        await using var db = _dbFactory.CreateDbContext();
        var items = await db.GameWeeks
            .AsNoTracking()
            .OrderBy(g => g.Season)
            .ThenBy(g => g.Number)
            .ToListAsync();

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
        await using var db = _dbFactory.CreateDbContext();
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

            var existing = await db.GameWeeks
                .FirstOrDefaultAsync(g => g.Season == season && g.Number == number);
            if (existing == null)
            {
                db.GameWeeks.Add(new GameWeek
                {
                    Season = season,
                    Number = number,
                    StartDate = start,
                    EndDate = end
                });
                added++;
            }
            else
            {
                existing.StartDate = start;
                existing.EndDate = end;
            }

            await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_all");
            await _cache.RemoveAsync($"{_prefix.Prefix}gameweeks_{season}");
            await _cache.RemoveAsync($"{_prefix.Prefix}gameweek_{season}_{number}");
            await _cache.RemoveAsync($"{_prefix.Prefix}next_{start:yyyy-MM-dd}");
            await _cache.RemoveAsync($"{_prefix.Prefix}next_{end:yyyy-MM-dd}");
        }

        await db.SaveChangesAsync();
        return added;
    }
}
