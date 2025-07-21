using Microsoft.EntityFrameworkCore;
using Predictorator.Data;
using Predictorator.Models;
using Predictorator.Services;
using System;
using System.Threading.Tasks;
using Xunit;

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
    public async Task AddOrUpdateAsync_adds_new_week()
    {
        var service = CreateService(out var db);
        var gw = new GameWeek { SeasonId = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) };

        await service.AddOrUpdateAsync(gw);

        Assert.Equal(1, await db.GameWeeks.CountAsync());
    }

    [Fact]
    public async Task AddOrUpdateAsync_updates_existing()
    {
        var service = CreateService(out var db);
        var gw = new GameWeek { SeasonId = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) };
        await service.AddOrUpdateAsync(gw);
        gw.EndDate = gw.EndDate.AddDays(1);

        await service.AddOrUpdateAsync(gw);

        var stored = await db.GameWeeks.FirstAsync();
        Assert.Equal(gw.EndDate, stored.EndDate);
    }

    [Fact]
    public async Task DeleteAsync_removes_week()
    {
        var service = CreateService(out var db);
        var gw = new GameWeek { SeasonId = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today };
        await service.AddOrUpdateAsync(gw);

        await service.DeleteAsync(gw);

        Assert.Empty(db.GameWeeks);
    }

    [Fact]
    public async Task GetGameWeekAsync_returns_week()
    {
        var service = CreateService(out var db);
        var gw = new GameWeek { SeasonId = "25-26", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today };
        await service.AddOrUpdateAsync(gw);

        var result = await service.GetGameWeekAsync("25-26", 1);

        Assert.NotNull(result);
        Assert.Equal(gw.StartDate, result!.StartDate);
    }
}
