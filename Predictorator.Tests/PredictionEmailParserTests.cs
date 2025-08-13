using Xunit;
using Predictorator.Core.Services;

namespace Predictorator.Tests;

public class PredictionEmailParserTests
{
    [Fact]
    public void Parse_ReturnsExpectedPredictions()
    {
        var text = @"Saturday, August 17, 2024
Arsenal 2 - 1 Chelsea

Sunday, August 18, 2024
Man City 0 - 0 Liverpool
";
        var result = PredictionEmailParser.Parse(text);

        Assert.Equal(2, result.Count);

        var first = result[0];
        Assert.Equal(new DateTime(2024, 8, 17), first.Date);
        Assert.Equal("Arsenal", first.HomeTeam);
        Assert.Equal(2, first.HomeScore);
        Assert.Equal(1, first.AwayScore);
        Assert.Equal("Chelsea", first.AwayTeam);

        var second = result[1];
        Assert.Equal(new DateTime(2024, 8, 18), second.Date);
        Assert.Equal("Man City", second.HomeTeam);
        Assert.Equal(0, second.HomeScore);
        Assert.Equal(0, second.AwayScore);
        Assert.Equal("Liverpool", second.AwayTeam);
    }
}
