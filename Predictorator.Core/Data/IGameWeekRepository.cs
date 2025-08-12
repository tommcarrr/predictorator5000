using Predictorator.Models;

namespace Predictorator.Data;

public interface IGameWeekRepository
{
    Task<List<GameWeek>> GetGameWeeksAsync(string? season = null);
    Task<GameWeek?> GetGameWeekAsync(string season, int number);
    Task<GameWeek?> GetByIdAsync(int id);
    Task<GameWeek?> GetNextGameWeekAsync(DateTime date);
    Task AddOrUpdateAsync(GameWeek gameWeek);
    Task DeleteAsync(int id);
}
