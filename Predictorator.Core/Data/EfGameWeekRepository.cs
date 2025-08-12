using Microsoft.EntityFrameworkCore;
using Predictorator.Models;

namespace Predictorator.Data;

public class EfGameWeekRepository : IGameWeekRepository
{
    private readonly ApplicationDbContext _db;

    public EfGameWeekRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
    {
        var query = _db.GameWeeks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(season))
            query = query.Where(g => g.Season == season);
        return query.OrderBy(g => g.Season).ThenBy(g => g.Number).ToListAsync();
    }

    public Task<GameWeek?> GetGameWeekAsync(string season, int number) =>
        _db.GameWeeks.AsNoTracking().FirstOrDefaultAsync(g => g.Season == season && g.Number == number);

    public Task<GameWeek?> GetByIdAsync(int id) =>
        _db.GameWeeks.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);

    public Task<GameWeek?> GetNextGameWeekAsync(DateTime date) =>
        _db.GameWeeks.Where(g => g.EndDate >= date)
            .OrderBy(g => g.StartDate)
            .FirstOrDefaultAsync();

    public async Task AddOrUpdateAsync(GameWeek gameWeek)
    {
        GameWeek? existing = null;
        if (gameWeek.Id != 0)
            existing = await _db.GameWeeks.FindAsync(gameWeek.Id);
        if (existing == null)
            existing = await _db.GameWeeks.FirstOrDefaultAsync(g => g.Season == gameWeek.Season && g.Number == gameWeek.Number);
        if (existing == null)
        {
            _db.GameWeeks.Add(gameWeek);
        }
        else
        {
            existing.Season = gameWeek.Season;
            existing.Number = gameWeek.Number;
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
