using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Predictorator.Components.Pages;
using Predictorator.Core.Models.Fixtures;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;
using System.Linq;
using MudBlazor;

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
            Assert.Equal("0", cut.Find("td[data-label='Points']").TextContent);
            Assert.Equal("Total Points: 0", cut.Find("p.total-points").TextContent);
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
            Assert.Equal(string.Empty, cut.Find("td[data-label='Points']").TextContent);
        });
    }

    [Fact]
    public async Task CalculatesPointsCorrectly()
    {
        var fixtureTime1 = new DateTime(2024, 1, 1, 15, 0, 0, DateTimeKind.Utc);
        var fixtureTime2 = new DateTime(2024, 1, 1, 17, 0, 0, DateTimeKind.Utc);
        var fixtures = new FixturesResponse
        {
            FromDate = fixtureTime1.Date,
            ToDate = fixtureTime2.Date,
            Response =
            [
                new FixtureData
                {
                    Fixture = new Fixture { Id = 1, Date = fixtureTime1, Venue = new Venue() },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Team A" },
                        Away = new Team { Name = "Team B" }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = 1, Away = 2 } }
                },
                new FixtureData
                {
                    Fixture = new Fixture { Id = 2, Date = fixtureTime2, Venue = new Venue() },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Team C" },
                        Away = new Team { Name = "Team D" }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = 2, Away = 1 } }
                }
            ]
        };

        const string text = "Monday, January 1, 2024\nTeam A 1 - 2 Team B\nTeam C 1 - 0 Team D";
        await using var ctx = CreateContext(fixtures, fixtureTime2.AddHours(4));
        var cut = ctx.Render<Parse>();
        cut.Find("input").Change("Bob");
        cut.Find("textarea").Change(text);
        cut.Find("button").Click();

        cut.WaitForAssertion(() =>
        {
            var pointCells = cut.FindAll("td[data-label='Points']");
            Assert.Equal("3", pointCells[0].TextContent);
            Assert.Equal("1", pointCells[1].TextContent);
            Assert.Equal("Total Points: 4", cut.Find("p.total-points").TextContent);
        });
    }

    [Fact]
    public async Task CopyToClipboard_Copies_All_Columns_And_Shows_Snackbar()
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
        var provider = ctx.Render<MudSnackbarProvider>();
        var cut = ctx.Render<Parse>();

        cut.Find("input").Change("Bob");
        cut.Find("textarea").Change(text);
        cut.FindAll("button").First(b => b.TextContent == "Parse").Click();
        cut.FindAll("button").First(b => b.TextContent == "Copy to Clipboard").Click();

        var invocation = ctx.JSInterop.Invocations.Single(i => i.Identifier == "navigator.clipboard.writeText");
        var lines = invocation.Arguments[0]?.ToString()?.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Name\tDate\tHome Team\tHome Prediction\tHome Actual\tAway Prediction\tAway Actual\tAway Team\tPoints", lines![0]);
        Assert.Equal("Bob\t01/01/2024\tTeam A\t1\t3\t2\t2\tTeam B\t0", lines[1]);

        provider.WaitForAssertion(() =>
        {
            var snackbar = provider.Find("div.mud-snackbar");
            Assert.Contains("Copied to clipboard", snackbar.TextContent);
        });
    }
}

