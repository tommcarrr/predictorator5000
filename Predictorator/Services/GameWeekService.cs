using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Options;

namespace Predictorator.Services;

public class GameWeekService : IGameWeekService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly HybridCache _cache;
    private readonly TimeSpan _cacheDuration;

    public GameWeekService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        HybridCache cache,
        IOptions<GameWeekCacheOptions> options)
    {
        _dbFactory = dbFactory;
        _cache = cache;
        _cacheDuration = TimeSpan.FromHours(options.Value.CacheDurationHours);
    }

    public Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
    {
        var cacheKey = $"gameweeks_{season ?? "all"}";
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
        var cacheKey = $"gameweek_{season}_{number}";
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
        var cacheKey = $"next_{date:yyyy-MM-dd}";
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

        await _cache.RemoveAsync("gameweeks_all");
        await _cache.RemoveAsync($"gameweeks_{gameWeek.Season}");
        await _cache.RemoveAsync($"gameweek_{gameWeek.Season}_{gameWeek.Number}");
        await _cache.RemoveAsync($"next_{gameWeek.StartDate:yyyy-MM-dd}");
        await _cache.RemoveAsync($"next_{gameWeek.EndDate:yyyy-MM-dd}");
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = _dbFactory.CreateDbContext();
        var entity = await db.GameWeeks.FindAsync(id);
        if (entity != null)
        {
            db.GameWeeks.Remove(entity);
            await db.SaveChangesAsync();
            await _cache.RemoveAsync("gameweeks_all");
            await _cache.RemoveAsync($"gameweeks_{entity.Season}");
            await _cache.RemoveAsync($"gameweek_{entity.Season}_{entity.Number}");
            await _cache.RemoveAsync($"next_{entity.StartDate:yyyy-MM-dd}");
            await _cache.RemoveAsync($"next_{entity.EndDate:yyyy-MM-dd}");
        }
    }
}
