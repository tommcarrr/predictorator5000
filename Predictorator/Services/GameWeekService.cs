using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Predictorator.Models;

namespace Predictorator.Services;

public class GameWeekService : IGameWeekService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public GameWeekService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var query = db.GameWeeks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(season))
            query = query.Where(g => g.Season == season);
        return query.OrderBy(g => g.Season).ThenBy(g => g.Number).ToListAsync();
    }

    public Task<GameWeek?> GetGameWeekAsync(string season, int number)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.GameWeeks
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Season == season && g.Number == number);
    }

    public Task<GameWeek?> GetNextGameWeekAsync(DateTime date)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.GameWeeks
            .Where(g => g.EndDate >= date)
            .OrderBy(g => g.StartDate)
            .FirstOrDefaultAsync();
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
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = _dbFactory.CreateDbContext();
        var entity = await db.GameWeeks.FindAsync(id);
        if (entity != null)
        {
            db.GameWeeks.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
