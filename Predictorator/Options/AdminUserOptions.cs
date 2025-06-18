namespace Predictorator.Options;

public class AdminUserOptions
{
    public const string SectionName = "AdminUser";

    public string Email { get; set; } = "admin@example.com";

    public string Password { get; set; } = "Admin123!";
}
