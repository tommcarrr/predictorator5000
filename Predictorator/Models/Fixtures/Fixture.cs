namespace Predictorator.Models.Fixtures;

public class Fixture
{
    public int Id { get; set; } = default!;
    public string Timezone { get; set; } = default!;
    public DateTime Date { get; set; } = default!;
    public long Timestamp { get; set; } = default!;
    public Periods Periods { get; set; } = default!;
    public Venue Venue { get; set; } = default!;
    public Status Status { get; set; } = default!;
}