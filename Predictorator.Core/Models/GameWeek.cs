namespace Predictorator.Models;

public class GameWeek
{
    public int Id { get; set; }
    public string Season { get; set; } = string.Empty; // e.g. "25-26"
    public int Number { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
