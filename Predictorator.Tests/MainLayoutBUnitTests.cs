using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;
using NSubstitute;
using System.Linq;
using Predictorator.Components.Layout;
using Predictorator.Components.Pages.Subscription;
using Predictorator.Services;
using MudBlazor;
using Xunit;

namespace Predictorator.Tests;

public class MainLayoutBUnitTests
{
    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddMudServices(c => c.PopoverOptions.CheckForPopoverProvider = false);
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<bool>("app.getDarkMode", Arg.Any<object[]?>()).Returns(new ValueTask<bool>(false));
        ctx.Services.AddSingleton<IJSRuntime>(jsRuntime);
        ctx.Services.AddSingleton(new BrowserInteropService(jsRuntime));
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        return ctx;
    }

    [Fact]
    public async Task LayoutRendersHeaderWithCorrectFont()
    {
        await using var ctx = CreateContext();
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var header = cut.Find("h5.mud-typography-h5");
        Assert.Equal("Predictotronix", header.TextContent.Trim());
    }

    [Fact]
    public async Task LayoutRendersSubscribeButton()
    {
        await using var ctx = CreateContext();
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b => b.TextContent.Trim() == "Subscribe");
    }

    [Fact]
    public async Task DarkModeToggleInvokesInterop()
    {
        await using var ctx = CreateContext();
        var jsRuntime = ctx.Services.GetRequiredService<IJSRuntime>();
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var toggle = cut.Find("#darkModeToggle");
        toggle.Click();
        _ = jsRuntime.Received().InvokeVoidAsync("app.saveDarkMode", Arg.Is<object[]>(o => (bool)o[0] == true));
    }

    [Fact]
    public async Task SubscribeButtonOpensDialog()
    {
        await using var ctx = CreateContext();
        var dialog = ctx.Services.GetRequiredService<IDialogService>();
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var button = cut.FindAll("button").First(b => b.TextContent.Trim() == "Subscribe");
        button.Click();
        dialog.Received().Show<Subscribe>(Arg.Any<string>());
    }
}
