using Predictorator.Core.Models.Fixtures;
using System.Linq;

namespace Predictorator.Core.Services;

public class PredictionProcessingService
{
    private readonly IFixtureService _fixtures;
    private readonly IDateTimeProvider _time;

    public PredictionProcessingService(IFixtureService fixtures, IDateTimeProvider time)
    {
        _fixtures = fixtures;
        _time = time;
    }

    public virtual async Task<IReadOnlyList<Prediction>> ProcessAsync(string? text)
    {
        var parsed = PredictionEmailParser.Parse(text)
            .Select(p => new Prediction
            {
                Date = p.Date,
                HomeTeam = p.HomeTeam,
                HomeScore = p.HomeScore,
                AwayScore = p.AwayScore,
                AwayTeam = p.AwayTeam
            })
            .ToList();

        if (parsed.Count == 0)
            return parsed;

        var from = parsed.Min(p => p.Date);
        var to = parsed.Max(p => p.Date);
        var fixtures = await _fixtures.GetFixturesAsync(from, to);
        var lookup = fixtures.Response.ToDictionary(
            f => (f.Fixture.Date.Date,
                  f.Teams.Home.Name.ToLowerInvariant(),
                  f.Teams.Away.Name.ToLowerInvariant()));

        DateTime? lastFixtureTime = null;
        foreach (var p in parsed)
        {
            var key = (p.Date.Date, p.HomeTeam.ToLowerInvariant(), p.AwayTeam.ToLowerInvariant());
            if (lookup.TryGetValue(key, out var fixture))
            {
                var fixtureDate = DateTime.SpecifyKind(fixture.Fixture.Date, DateTimeKind.Utc);
                if (lastFixtureTime == null || fixtureDate > lastFixtureTime)
                    lastFixtureTime = fixtureDate;
            }
        }

        if (lastFixtureTime.HasValue && _time.UtcNow >= lastFixtureTime.Value.AddHours(3))
        {
            foreach (var p in parsed)
            {
                var key = (p.Date.Date, p.HomeTeam.ToLowerInvariant(), p.AwayTeam.ToLowerInvariant());
                if (lookup.TryGetValue(key, out var fixture))
                {
                    p.ActualHomeScore = fixture.Score?.Fulltime.Home;
                    p.ActualAwayScore = fixture.Score?.Fulltime.Away;
                    if (p.ActualHomeScore.HasValue && p.ActualAwayScore.HasValue)
                    {
                        if (p.HomeScore == p.ActualHomeScore && p.AwayScore == p.ActualAwayScore)
                            p.Points = 3;
                        else
                        {
                            var predictedResult = Math.Sign(p.HomeScore - p.AwayScore);
                            var actualResult = Math.Sign(p.ActualHomeScore.Value - p.ActualAwayScore.Value);
                            if (predictedResult == actualResult)
                                p.Points = 1;
                        }
                    }
                }
            }
        }

        return parsed;
    }

    public class Prediction
    {
        public DateTime Date { get; init; }
        public string HomeTeam { get; init; } = string.Empty;
        public int HomeScore { get; init; }
        public int AwayScore { get; init; }
        public string AwayTeam { get; init; } = string.Empty;
        public int? ActualHomeScore { get; set; }
        public int? ActualAwayScore { get; set; }
        public int Points { get; set; }
    }
}
