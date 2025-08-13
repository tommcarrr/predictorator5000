namespace Predictorator.Core.Options;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public string[] ExcludedIPs { get; set; } = Array.Empty<string>();
}
