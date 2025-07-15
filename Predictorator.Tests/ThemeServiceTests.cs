using System.Threading.Tasks;
using NSubstitute;
using Xunit;
using Predictorator.Services;
using Microsoft.JSInterop;

namespace Predictorator.Tests;

public class ThemeServiceTests
{
    [Fact]
    public async Task InitializeAsync_Raises_OnChange()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<string?>("app.getLocalStorage", Arg.Any<object[]>()).Returns("true");
        var browser = new BrowserInteropService(jsRuntime);
        var service = new ThemeService(browser);
        bool raised = false;
        service.OnChange += () => raised = true;
        await service.InitializeAsync();
        Assert.True(raised);
    }
}
