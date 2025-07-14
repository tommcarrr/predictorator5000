using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Predictorator.Components.Layout;
using Predictorator.Components.Pages.Subscription;
using Predictorator.Services;

namespace Predictorator.Tests;

public class MainLayoutBUnitTests
{
    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var jsRuntime = Substitute.For<IJSRuntime>();
        ctx.Services.AddSingleton(jsRuntime);
        var browser = new BrowserInteropService(jsRuntime);
        ctx.Services.AddSingleton(browser);
        var theme = new ThemeService();
        ctx.Services.AddSingleton(theme);
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
    public async Task DarkModeToggleUpdatesState()
    {
        await using var ctx = CreateContext();
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var toggle = cut.Find("#darkModeToggle");
        toggle.Click();
        cut.WaitForAssertion(() =>
        {
            Assert.True(ctx.Services.GetRequiredService<ThemeService>().IsDarkMode);
        });
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
