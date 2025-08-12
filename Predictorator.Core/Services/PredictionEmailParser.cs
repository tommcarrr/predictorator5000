using System.Globalization;
using System.Text.RegularExpressions;

namespace Predictorator.Services;

public static class PredictionEmailParser
{
    private const string DateFormat = "dddd, MMMM d, yyyy";

    public static IReadOnlyList<ParsedPrediction> Parse(string? text)
    {
        var result = new List<ParsedPrediction>();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        DateTime? currentDate = null;
        var lines = text.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (DateTime.TryParseExact(line, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                currentDate = date.Date;
                continue;
            }

            if (!currentDate.HasValue)
                continue;

            var match = Regex.Match(line, @"^(.*?)\s+(\d+)\s*-\s*(\d+)\s+(.*)$");
            if (!match.Success)
                continue;

            var homeTeam = match.Groups[1].Value.Trim();
            var homeScore = int.Parse(match.Groups[2].Value);
            var awayScore = int.Parse(match.Groups[3].Value);
            var awayTeam = match.Groups[4].Value.Trim();

            result.Add(new ParsedPrediction(currentDate.Value, homeTeam, homeScore, awayScore, awayTeam));
        }

        return result;
    }

    public record ParsedPrediction(DateTime Date, string HomeTeam, int HomeScore, int AwayScore, string AwayTeam);
}
