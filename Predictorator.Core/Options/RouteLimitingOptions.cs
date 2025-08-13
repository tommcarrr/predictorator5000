namespace Predictorator.Core.Options;

public class RouteLimitingOptions
{
    public const string SectionName = "RouteLimiting";
    public int UniqueRouteLimit { get; set; } = 50;
}
