using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class ThemeServiceTests
{
    [Fact]
    public async Task InitializeAsync_Raises_OnChange()
    {
        var storage = new FakeBrowserStorage();
        await storage.SetAsync("ceefaxMode", true);
        var service = new ThemeService(storage);
        bool raised = false;
        service.OnChange += () => raised = true;
        await service.InitializeAsync();
        Assert.True(raised);
    }
}
