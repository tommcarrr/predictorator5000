namespace Predictorator.Models;

public record LoginRequest(string Email, string Password, bool RememberMe);
