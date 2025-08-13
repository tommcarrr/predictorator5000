using Predictorator.Core.Data;
using Predictorator.Core.Models;

namespace Predictorator.Tests.Helpers;

public class InMemoryGameWeekRepository : IGameWeekRepository
{
    public List<GameWeek> Items { get; } = new();
    private int _nextId = 1;

    public Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
    {
        var query = Items.AsQueryable();
        if (!string.IsNullOrEmpty(season))
            query = query.Where(g => g.Season == season);
        return Task.FromResult(query.OrderBy(g => g.Season).ThenBy(g => g.Number).ToList());
    }

    public Task<GameWeek?> GetGameWeekAsync(string season, int number) =>
        Task.FromResult(Items.FirstOrDefault(g => g.Season == season && g.Number == number));

    public Task<GameWeek?> GetByIdAsync(int id) =>
        Task.FromResult(Items.FirstOrDefault(g => g.Id == id));

    public Task<GameWeek?> GetNextGameWeekAsync(DateTime date) =>
        Task.FromResult(Items.Where(g => g.EndDate >= date)
            .OrderBy(g => g.StartDate)
            .FirstOrDefault());

    public Task AddOrUpdateAsync(GameWeek gameWeek)
    {
        GameWeek? existing = null;
        if (gameWeek.Id != 0)
            existing = Items.FirstOrDefault(g => g.Id == gameWeek.Id);
        if (existing == null)
            existing = Items.FirstOrDefault(g => g.Season == gameWeek.Season && g.Number == gameWeek.Number);
        if (existing == null)
        {
            gameWeek.Id = _nextId++;
            Items.Add(gameWeek);
        }
        else
        {
            existing.Season = gameWeek.Season;
            existing.Number = gameWeek.Number;
            existing.StartDate = gameWeek.StartDate;
            existing.EndDate = gameWeek.EndDate;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        Items.RemoveAll(g => g.Id == id);
        return Task.CompletedTask;
    }
}
