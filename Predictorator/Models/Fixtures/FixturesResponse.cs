public class FixturesResponse
{
    public string Get { get; set; }
    public Parameters Parameters { get; set; }
    public List<object> Errors { get; set; }
    public int Results { get; set; }
    public Paging Paging { get; set; }
    public List<FixtureData> Response { get; set; }
}