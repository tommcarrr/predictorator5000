using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Predictorator.Models;

namespace Predictorator.Services;

public class GameWeekService : IGameWeekService
{
    private readonly ApplicationDbContext _db;

    public GameWeekService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
    {
        var query = _db.GameWeeks.AsQueryable();
        if (!string.IsNullOrEmpty(season))
            query = query.Where(g => g.Season == season);
        return query.OrderBy(g => g.Season).ThenBy(g => g.Number).ToListAsync();
    }

    public Task<GameWeek?> GetGameWeekAsync(string season, int number)
    {
        return _db.GameWeeks
            .FirstOrDefaultAsync(g => g.Season == season && g.Number == number);
    }

    public async Task AddOrUpdateAsync(GameWeek gameWeek)
    {
        var existing = await _db.GameWeeks
            .FirstOrDefaultAsync(g => g.Season == gameWeek.Season && g.Number == gameWeek.Number);
        if (existing == null)
        {
            _db.GameWeeks.Add(gameWeek);
        }
        else
        {
            existing.StartDate = gameWeek.StartDate;
            existing.EndDate = gameWeek.EndDate;
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _db.GameWeeks.FindAsync(id);
        if (entity != null)
        {
            _db.GameWeeks.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
