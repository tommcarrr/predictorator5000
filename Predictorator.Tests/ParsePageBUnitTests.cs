using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Predictorator.Components.Pages;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class ParsePageBUnitTests
{
    private BunitContext CreateContext(FixturesResponse fixtures, DateTime now)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));
        ctx.Services.AddSingleton<IDateTimeProvider>(new FakeDateTimeProvider { UtcNow = now, Today = now.Date });
        return ctx;
    }

    [Fact]
    public async Task ShowsActualScores_WhenPastThreshold()
    {
        var fixtureTime = new DateTime(2024, 1, 1, 15, 0, 0, DateTimeKind.Utc);
        var fixtures = new FixturesResponse
        {
            FromDate = fixtureTime.Date,
            ToDate = fixtureTime.Date,
            Response =
            [
                new FixtureData
                {
                    Fixture = new Fixture { Id = 1, Date = fixtureTime, Venue = new Venue() },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Team A" },
                        Away = new Team { Name = "Team B" }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = 3, Away = 2 } }
                }
            ]
        };

        const string text = "Monday, January 1, 2024\nTeam A 1 - 2 Team B";
        await using var ctx = CreateContext(fixtures, fixtureTime.AddHours(4));
        var cut = ctx.Render<Parse>();
        cut.Find("input").Change("Bob");
        cut.Find("textarea").Change(text);
        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("3", cut.Find("td[data-label='Home Actual']").TextContent);
            Assert.Equal("2", cut.Find("td[data-label='Away Actual']").TextContent);
        });
    }

    [Fact]
    public async Task DoesNotShowScores_BeforeThreshold()
    {
        var fixtureTime = new DateTime(2024, 1, 1, 15, 0, 0, DateTimeKind.Utc);
        var fixtures = new FixturesResponse
        {
            FromDate = fixtureTime.Date,
            ToDate = fixtureTime.Date,
            Response =
            [
                new FixtureData
                {
                    Fixture = new Fixture { Id = 1, Date = fixtureTime, Venue = new Venue() },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Team A" },
                        Away = new Team { Name = "Team B" }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = 3, Away = 2 } }
                }
            ]
        };

        const string text = "Monday, January 1, 2024\nTeam A 1 - 2 Team B";
        await using var ctx = CreateContext(fixtures, fixtureTime.AddHours(2));
        var cut = ctx.Render<Parse>();
        cut.Find("input").Change("Bob");
        cut.Find("textarea").Change(text);
        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(string.Empty, cut.Find("td[data-label='Home Actual']").TextContent);
            Assert.Equal(string.Empty, cut.Find("td[data-label='Away Actual']").TextContent);
        });
    }
}

