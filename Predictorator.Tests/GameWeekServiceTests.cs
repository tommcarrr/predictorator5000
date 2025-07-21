using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Options;
using Predictorator.Services;

namespace Predictorator.Tests;

public class GameWeekServiceTests
{
    private class TestFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        public int CreateCount { get; private set; }

        public TestFactory(DbContextOptions<ApplicationDbContext> options)
        {
            _options = options;
        }

        public ApplicationDbContext CreateDbContext()
        {
            CreateCount++;
            return new ApplicationDbContext(_options);
        }
    }

    private static GameWeekService CreateService(out ApplicationDbContext db, out TestFactory factory)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        factory = new TestFactory(options);
        db = factory.CreateDbContext();
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.Configure<GameWeekCacheOptions>(_ => { });
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        var opts = provider.GetRequiredService<IOptions<GameWeekCacheOptions>>();
        var prefix = new CachePrefixService();
        return new GameWeekService(factory, cache, prefix, opts);
    }

    [Fact]
    public async Task AddOrUpdateAsync_adds_new_gameweek()
    {
        var service = CreateService(out var db, out var factory);
        var gw = new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) };

        await service.AddOrUpdateAsync(gw);
        await using var verifyAdd = factory.CreateDbContext();
        Assert.Single(verifyAdd.GameWeeks);
    }

    [Fact]
    public async Task AddOrUpdateAsync_updates_existing()
    {
        var service = CreateService(out var db, out var factory);
        db.GameWeeks.Add(new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) });
        await db.SaveChangesAsync();

        var gw = new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today.AddDays(7), EndDate = DateTime.Today.AddDays(13) };
        await service.AddOrUpdateAsync(gw);

        await using var verify = factory.CreateDbContext();
        var updated = await verify.GameWeeks.FirstAsync();
        Assert.Equal(gw.StartDate, updated.StartDate);
        Assert.Equal(gw.EndDate, updated.EndDate);
    }

    [Fact]
    public async Task DeleteAsync_removes_item()
    {
        var service = CreateService(out var db, out var factory);
        var gw = new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today };
        db.GameWeeks.Add(gw);
        await db.SaveChangesAsync();

        await service.DeleteAsync(gw.Id);

        await using var verifyDelete = factory.CreateDbContext();
        Assert.Empty(verifyDelete.GameWeeks);
    }

    [Fact]
    public async Task GetNextGameWeekAsync_returns_next_week()
    {
        var service = CreateService(out var db, out var factory);
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
        var service = CreateService(out var db, out var factory);
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

        await using var verifyUpdate = factory.CreateDbContext();
        var dbItem = await verifyUpdate.GameWeeks.SingleAsync();
        Assert.Equal(updated.Season, dbItem.Season);
        Assert.Equal(updated.Number, dbItem.Number);
        Assert.Equal(updated.StartDate, dbItem.StartDate);
        Assert.Equal(updated.EndDate, dbItem.EndDate);
    }

    [Fact]
    public async Task GetGameWeeksAsync_uses_cache()
    {
        var service = CreateService(out var db, out var factory);
        db.GameWeeks.Add(new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) });
        await db.SaveChangesAsync();

        var before = factory.CreateCount;
        _ = await service.GetGameWeeksAsync();
        var afterFirst = factory.CreateCount;
        _ = await service.GetGameWeeksAsync();
        var afterSecond = factory.CreateCount;

        Assert.Equal(before + 1, afterFirst);
        Assert.Equal(afterFirst, afterSecond);
    }
}
