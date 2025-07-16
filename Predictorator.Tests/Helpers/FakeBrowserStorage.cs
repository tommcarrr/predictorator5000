using Predictorator.Services;

namespace Predictorator.Tests.Helpers;

public class FakeBrowserStorage : IBrowserStorage
{
    private readonly Dictionary<string, bool> _storage = new();

    public Task SetAsync(string key, bool value)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public Task<bool?> GetAsync(string key)
    {
        return Task.FromResult(_storage.TryGetValue(key, out var value) ? (bool?)value : null);
    }
}
