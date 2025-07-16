using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Predictorator.Services;

public class ProtectedLocalStorageBrowserStorage : IBrowserStorage
{
    private readonly ProtectedLocalStorage _storage;

    public ProtectedLocalStorageBrowserStorage(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    public async Task SetAsync(string key, bool value)
    {
        await _storage.SetAsync(key, value);
    }

    public async Task<bool?> GetAsync(string key)
    {
        var result = await _storage.GetAsync<bool>(key);
        return result.Success ? result.Value : null;
    }
}
