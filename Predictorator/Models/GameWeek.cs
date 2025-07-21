using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Predictorator.Models;

public class GameWeek
{
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string SeasonId { get; set; } = string.Empty;
    public Season? Season { get; set; }

    public int Number { get; set; }

    [Column(TypeName = "date")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime EndDate { get; set; }
}
