namespace Predictorator.Models.Fixtures;

public class FixturesResponse
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<FixtureData> Response { get; set; } = [];
}