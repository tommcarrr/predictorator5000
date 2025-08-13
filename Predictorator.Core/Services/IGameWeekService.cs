using System.IO;
using Predictorator.Core.Models;

namespace Predictorator.Core.Services;

public interface IGameWeekService
{
    Task<List<GameWeek>> GetGameWeeksAsync(string? season = null);
    Task<GameWeek?> GetGameWeekAsync(string season, int number);
    Task<GameWeek?> GetNextGameWeekAsync(DateTime date);
    Task AddOrUpdateAsync(GameWeek gameWeek);
    Task DeleteAsync(int id);
    Task<string> ExportCsvAsync();
    Task<int> ImportCsvAsync(Stream csv);
}
