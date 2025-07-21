using Predictorator.Models;
using Predictorator.Services;

namespace Predictorator.Tests.Helpers;

public class FakeGameWeekService : IGameWeekService
{
    public List<GameWeek> Items { get; set; } = new();

    public Task AddOrUpdateAsync(GameWeek gameWeek)
    {
        var existing = Items.FirstOrDefault(g => g.Season == gameWeek.Season && g.Number == gameWeek.Number);
        if (existing == null)
            Items.Add(gameWeek);
        else
        {
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

    public Task<GameWeek?> GetGameWeekAsync(string season, int number)
    {
        return Task.FromResult<GameWeek?>(Items.FirstOrDefault(g => g.Season == season && g.Number == number));
    }

    public Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
    {
        var query = Items.AsEnumerable();
        if (!string.IsNullOrEmpty(season))
            query = query.Where(g => g.Season == season);
        return Task.FromResult(query.ToList());
    }
}
