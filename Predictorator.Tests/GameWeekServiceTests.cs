using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Services;

namespace Predictorator.Tests;

public class GameWeekServiceTests
{
    private static GameWeekService CreateService(out ApplicationDbContext db)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new ApplicationDbContext(options);
        return new GameWeekService(db);
    }

    [Fact]
    public async Task AddOrUpdateAsync_adds_new_gameweek()
    {
        var service = CreateService(out var db);
        var gw = new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) };

        await service.AddOrUpdateAsync(gw);

        Assert.Single(db.GameWeeks);
    }

    [Fact]
    public async Task AddOrUpdateAsync_updates_existing()
    {
        var service = CreateService(out var db);
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
        var service = CreateService(out var db);
        var gw = new GameWeek { Season = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today };
        db.GameWeeks.Add(gw);
        await db.SaveChangesAsync();

        await service.DeleteAsync(gw.Id);

        Assert.Empty(db.GameWeeks);
    }

    [Fact]
    public async Task GetNextGameWeekAsync_returns_next_week()
    {
        var service = CreateService(out var db);
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
        var service = CreateService(out var db);
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
}
