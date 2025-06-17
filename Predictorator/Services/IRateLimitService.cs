namespace Predictorator.Services;

public interface IRateLimitService
{
    bool ShouldLimit(string ipAddress);
}
