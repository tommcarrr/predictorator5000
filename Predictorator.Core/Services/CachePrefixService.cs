using System.Threading;

namespace Predictorator.Services;

public sealed class CachePrefixService
{
    private string _prefix = string.Empty;

    public string Prefix => _prefix;

    public void Clear()
    {
        Interlocked.Exchange(ref _prefix, Guid.NewGuid().ToString("N") + "_");
    }
}
