using Predictorator.Core.Models;
using Predictorator.Core.Services;
using System.IO;
using System.Linq;
using System.Text;

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

    public Task<GameWeek?> GetNextGameWeekAsync(DateTime date)
    {
        var result = Items
            .Where(g => g.EndDate >= date)
            .OrderBy(g => g.StartDate)
            .FirstOrDefault();
        return Task.FromResult<GameWeek?>(result);
    }

    public Task<string> ExportCsvAsync()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Season,Number,StartDate,EndDate");
        foreach (var g in Items)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                g.Season,
                g.Number.ToString(),
                g.StartDate.ToString("O"),
                g.EndDate.ToString("O")
            }));
        }
        return Task.FromResult(sb.ToString());
    }

    public async Task<int> ImportCsvAsync(Stream csv)
    {
        using var reader = new StreamReader(csv, Encoding.UTF8, leaveOpen: true);
        string? line;
        var added = 0;
        var first = true;
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
            if (!DateTime.TryParse(parts[2], out var start)) continue;
            if (!DateTime.TryParse(parts[3], out var end)) continue;

            var existing = Items.FirstOrDefault(g => g.Season == season && g.Number == number);
            if (existing != null)
            {
                continue;
            }

            Items.Add(new GameWeek { Season = season, Number = number, StartDate = start, EndDate = end });
            added++;
        }

        return added;
    }
}
