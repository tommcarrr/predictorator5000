using Predictorator.Core.Models.Fixtures;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class PredictionProcessingServiceTests
{
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
        var service = new PredictionProcessingService(
            new FakeFixtureService(fixtures),
            new FakeDateTimeProvider { UtcNow = fixtureTime.AddHours(4) });
        const string text = "Monday, January 1, 2024\nTeam A 1 - 2 Team B";

        var result = await service.ProcessAsync(text);

        var prediction = Assert.Single(result);
        Assert.Equal(3, prediction.ActualHomeScore);
        Assert.Equal(2, prediction.ActualAwayScore);
        Assert.Equal(0, prediction.Points);
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
        var service = new PredictionProcessingService(
            new FakeFixtureService(fixtures),
            new FakeDateTimeProvider { UtcNow = fixtureTime.AddHours(2) });
        const string text = "Monday, January 1, 2024\nTeam A 1 - 2 Team B";

        var result = await service.ProcessAsync(text);

        var prediction = Assert.Single(result);
        Assert.Null(prediction.ActualHomeScore);
        Assert.Null(prediction.ActualAwayScore);
        Assert.Equal(0, prediction.Points);
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
        var service = new PredictionProcessingService(
            new FakeFixtureService(fixtures),
            new FakeDateTimeProvider { UtcNow = fixtureTime2.AddHours(4) });
        const string text = "Monday, January 1, 2024\nTeam A 1 - 2 Team B\nTeam C 1 - 0 Team D";

        var result = await service.ProcessAsync(text);

        Assert.Equal(2, result.Count);
        Assert.Equal(3, result[0].Points);
        Assert.Equal(1, result[1].Points);
        Assert.Equal(4, result.Sum(p => p.Points));
    }
}
