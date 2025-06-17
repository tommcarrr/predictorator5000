namespace Predictorator.Models.Fixtures;

public class Score
{
    public ScoreHomeAway Halftime { get; set; } = default!;
    public ScoreHomeAway Fulltime { get; set; } = default!;
    public ScoreHomeAway Extratime { get; set; } = default!;
    public Penalty Penalty { get; set; } = default!;
}