namespace Predictorator.Core.Models.Fixtures;

public class Fixture
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public Venue Venue { get; set; } = new();
}