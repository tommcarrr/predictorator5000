public class FixturesResponse
{
    
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Get { get; set; }
    public Parameters Parameters { get; set; }
    public List<object> Errors { get; set; }
    public int Results { get; set; }
    public Paging Paging { get; set; }
    public List<FixtureData> Response { get; set; }
}