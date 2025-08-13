using Predictorator.Core.Services;

namespace Predictorator.Tests;

public class CachePrefixServiceTests
{
    [Fact]
    public void Prefix_is_empty_until_cleared()
    {
        var svc = new CachePrefixService();
        Assert.Equal(string.Empty, svc.Prefix);
    }

    [Fact]
    public void Clear_generates_new_prefix_each_time()
    {
        var svc = new CachePrefixService();
        svc.Clear();
        var first = svc.Prefix;
        svc.Clear();
        var second = svc.Prefix;
        Assert.NotEqual(string.Empty, first);
        Assert.EndsWith("_", first);
        Assert.EndsWith("_", second);
        Assert.NotEqual(first, second);
    }
}
