using System.ComponentModel.DataAnnotations;

namespace Predictorator.Models;

public class Subscriber
{
    public int Id { get; set; }

    [EmailAddress]
    public required string Email { get; set; }

    public bool Verified { get; set; }

    public Guid Token { get; set; }
}
