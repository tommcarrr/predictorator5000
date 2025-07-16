namespace Predictorator.Services;

public interface IBrowserStorage
{
    Task SetAsync(string key, bool value);
    Task<bool?> GetAsync(string key);
}
