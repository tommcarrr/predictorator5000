using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Predictorator.Models;

namespace Predictorator.Services;

public class GameWeekService
{
    private readonly ApplicationDbContext _db;

    public GameWeekService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Season>> GetSeasonsAsync()
    {
        return await _db.Seasons
            .Include(s => s.GameWeeks.OrderBy(g => g.Number))
            .OrderBy(s => s.Id)
            .ToListAsync();
    }

    public async Task<Season?> GetSeasonAsync(string id)
    {
        return await _db.Seasons
            .Include(s => s.GameWeeks.OrderBy(g => g.Number))
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<GameWeek?> GetGameWeekAsync(string seasonId, int number)
    {
        return await _db.GameWeeks
            .FirstOrDefaultAsync(g => g.SeasonId == seasonId && g.Number == number);
    }

    public async Task AddOrUpdateAsync(GameWeek gameWeek)
    {
        var existing = await _db.GameWeeks
            .FirstOrDefaultAsync(g => g.SeasonId == gameWeek.SeasonId && g.Number == gameWeek.Number);
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

    public async Task DeleteAsync(GameWeek gameWeek)
    {
        _db.GameWeeks.Remove(gameWeek);
        await _db.SaveChangesAsync();
    }
}
