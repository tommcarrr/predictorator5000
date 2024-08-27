namespace Predictorator.Models.Fixtures;

public class Fixture
{
    public int Id { get; set; }
    public string Timezone { get; set; }
    public DateTime Date { get; set; }
    public long Timestamp { get; set; }
    public Periods Periods { get; set; }
    public Venue Venue { get; set; }
    public Status Status { get; set; }
}