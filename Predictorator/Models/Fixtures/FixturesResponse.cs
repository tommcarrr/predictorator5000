namespace Predictorator.Models.Fixtures;

public class FixturesResponse
{

    public DateTime FromDate { get; set; } = default!;
    public DateTime ToDate { get; set; } = default!;
    public string Get { get; set; } = default!;
    public Parameters Parameters { get; set; } = default!;
    public List<object> Errors { get; set; } = default!;
    public int Results { get; set; } = default!;
    public Paging Paging { get; set; } = default!;
    public int? CurrentWeekOffset { get; set; } = default!;
    public bool AutoWeek { get; set; } = default!;
    public List<FixtureData> Response { get; set; } = default!;
}