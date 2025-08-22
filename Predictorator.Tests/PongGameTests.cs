using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Predictorator.Tests;

public class PongGameTests
{
    [Fact]
    public void SpeedIncrease_Should_Be_Greater_Than_BaseValue()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(root, "Predictorator", "wwwroot", "js", "site.js");
        Assert.True(File.Exists(scriptPath), "site.js not found");

        var script = File.ReadAllText(scriptPath);
        var match = Regex.Match(script, @"const speedIncrease = ([0-9.]+);");
        Assert.True(match.Success, "speedIncrease constant not found");

        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.True(value >= 1.1, $"Expected speedIncrease >= 1.1 but was {value}");
    }
}

