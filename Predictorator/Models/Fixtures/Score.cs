namespace Predictorator.Models.Fixtures;

public class Score
{
    public ScoreHomeAway Halftime { get; set; }
    public ScoreHomeAway Fulltime { get; set; }
    public ScoreHomeAway Extratime { get; set; }
    public Penalty Penalty { get; set; }
}