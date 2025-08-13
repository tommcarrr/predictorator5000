namespace Predictorator.Core.Options;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; }
    public string[] ExcludedIPs { get; set; } = Array.Empty<string>();
}
