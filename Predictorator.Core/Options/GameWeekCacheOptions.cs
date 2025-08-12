namespace Predictorator.Options;

public class GameWeekCacheOptions
{
    public const string SectionName = "GameWeekCache";

    /// <summary>
    /// Cache duration in hours.
    /// </summary>
    public int CacheDurationHours { get; set; } = 2;
}
