using System.ComponentModel.DataAnnotations;

namespace Predictorator.Models;

public class Season
{
    [Key]
    [MaxLength(10)]
    public string Id { get; set; } = string.Empty; // e.g., "25-26"

    public ICollection<GameWeek> GameWeeks { get; set; } = new List<GameWeek>();
}
