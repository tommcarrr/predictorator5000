using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Options;
using Predictorator.Services;
using System.IO;
using System.Linq;
using System.Text;

namespace Predictorator.Tests;

public class GameWeekServiceTests
{
    private class CountingRepository : IGameWeekRepository
    {
        private readonly IGameWeekRepository _inner;
        public int GetGameWeeksCount { get; private set; }

        public CountingRepository(IGameWeekRepository inner)
        {
            _inner = inner;
        }

        public Task<List<GameWeek>> GetGameWeeksAsync(string? season = null)
        {
            GetGameWeeksCount++;
            return _inner.GetGameWeeksAsync(season);
        }

        public Task<GameWeek?> GetGameWeekAsync(string season, int number) => _inner.GetGameWeekAsync(season, number);
        public Task<GameWeek?> GetByIdAsync(int id) => _inner.GetByIdAsync(id);
        public Task<GameWeek?> GetNextGameWeekAsync(DateTime date) => _inner.GetNextGameWeekAsync(date);
        public Task AddOrUpdateAsync(GameWeek gameWeek) => _inner.AddOrUpdateAsync(gameWeek);
        public Task DeleteAsync(int id) => _inner.DeleteAsync(id);
    }

    private static GameWeekService CreateService(out ApplicationDbContext db, out CountingRepository repo)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new ApplicationDbContext(options);
        var inner = new EfGameWeekRepository(db);
        repo = new CountingRepository(inner);
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.Configure<GameWeekCacheOptions>(_ => { });
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var opts = provider.GetRequiredService<IOptions<GameWeekCacheOptions>>();
        var prefix = new CachePrefixService();
        return new GameWeekService(repo, cache, prefix, opts);
    }

    [Fact]
    public async Task AddOrUpdateAsync_adds_new_gameweek()
    {
        var service = CreateService(out var db, out var repo);
        var gw = new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) };

        await service.AddOrUpdateAsync(gw);
        Assert.Single(db.GameWeeks);
    }

    [Fact]
    public async Task AddOrUpdateAsync_updates_existing()
    {
        var service = CreateService(out var db, out var repo);
        db.GameWeeks.Add(new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) });
        await db.SaveChangesAsync();

        var gw = new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today.AddDays(7), EndDate = DateTime.Today.AddDays(13) };
        await service.AddOrUpdateAsync(gw);

        var updated = await db.GameWeeks.FirstAsync();
        Assert.Equal(gw.StartDate, updated.StartDate);
        Assert.Equal(gw.EndDate, updated.EndDate);
    }

    [Fact]
    public async Task DeleteAsync_removes_item()
    {
        var service = CreateService(out var db, out var repo);
        var gw = new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today };
        db.GameWeeks.Add(gw);
        await db.SaveChangesAsync();

        await service.DeleteAsync(gw.Id);
        Assert.Empty(db.GameWeeks);
    }

    [Fact]
    public async Task GetNextGameWeekAsync_returns_next_week()
    {
        var service = CreateService(out var db, out var repo);
        db.GameWeeks.AddRange(
            new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today.AddDays(-14), EndDate = DateTime.Today.AddDays(-8) },
            new GameWeek { Season = "25-26", Number = 2, StartDate = DateTime.Today.AddDays(-7), EndDate = DateTime.Today.AddDays(-1) },
            new GameWeek { Season = "25-26", Number = 3, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) }
        );
        await db.SaveChangesAsync();

        var result = await service.GetNextGameWeekAsync(DateTime.Today);

        Assert.NotNull(result);
        Assert.Equal(3, result!.Number);
    }

    [Fact]
    public async Task AddOrUpdateAsync_updates_when_key_changes()
    {
        var service = CreateService(out var db, out var repo);
        var gw = new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) };
        db.GameWeeks.Add(gw);
        await db.SaveChangesAsync();

        var updated = new GameWeek
        {
            Id = gw.Id,
            Season = "26-27",
            Number = 2,
            StartDate = DateTime.Today.AddDays(7),
            EndDate = DateTime.Today.AddDays(13)
        };

        await service.AddOrUpdateAsync(updated);

        var dbItem = await db.GameWeeks.SingleAsync();
        Assert.Equal(updated.Season, dbItem.Season);
        Assert.Equal(updated.Number, dbItem.Number);
        Assert.Equal(updated.StartDate, dbItem.StartDate);
        Assert.Equal(updated.EndDate, dbItem.EndDate);
    }

    [Fact]
    public async Task GetGameWeeksAsync_uses_cache()
    {
        var service = CreateService(out var db, out var repo);
        db.GameWeeks.Add(new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) });
        await db.SaveChangesAsync();

        var before = repo.GetGameWeeksCount;
        _ = await service.GetGameWeeksAsync();
        var afterFirst = repo.GetGameWeeksCount;
        _ = await service.GetGameWeeksAsync();
        var afterSecond = repo.GetGameWeeksCount;

        Assert.Equal(before + 1, afterFirst);
        Assert.Equal(afterFirst, afterSecond);
    }

    [Fact]
    public async Task ExportCsvAsync_exports_all()
    {
        var service = CreateService(out var db, out _);
        var now = DateTime.UtcNow.Date;
        db.GameWeeks.AddRange(
            new GameWeek { Season = "25-26", Number = 1, StartDate = now, EndDate = now.AddDays(6) },
            new GameWeek { Season = "25-26", Number = 2, StartDate = now.AddDays(7), EndDate = now.AddDays(13) }
        );
        await db.SaveChangesAsync();

        var csv = await service.ExportCsvAsync();
        var lines = csv.Trim().Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Contains("25-26,1", lines[1]);
        Assert.Contains("25-26,2", lines[2]);
    }

    [Fact]
    public async Task ImportCsvAsync_adds_and_updates()
    {
        var service = CreateService(out var db, out var repo);
        var now = DateTime.UtcNow.Date;
        db.GameWeeks.Add(new GameWeek { Season = "25-26", Number = 1, StartDate = now, EndDate = now.AddDays(6) });
        await db.SaveChangesAsync();

        var csv = "Season,Number,StartDate,EndDate\n" +
                  $"25-26,1,{now.AddDays(1):O},{now.AddDays(7):O}\n" +
                  $"25-26,2,{now.AddDays(8):O},{now.AddDays(14):O}\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var added = await service.ImportCsvAsync(ms);

        Assert.Equal(1, added);
        var all = await db.GameWeeks.ToListAsync();
        Assert.Equal(2, all.Count);
        var gw1 = all.Single(g => g.Number == 1);
        Assert.Equal(now.AddDays(1), gw1.StartDate);
    }
}
