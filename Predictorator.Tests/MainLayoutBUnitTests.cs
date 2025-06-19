using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;
using Predictorator.Components.Layout;
using Predictorator.Services;
using MudBlazor;
using Xunit;

namespace Predictorator.Tests;

public class MainLayoutBUnitTests : BunitContext
{
    public MainLayoutBUnitTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<bool>("app.getDarkMode", Arg.Any<object[]?>()).Returns(new ValueTask<bool>(false));
        Services.AddSingleton(new BrowserInteropService(jsRuntime));
        Services.AddSingleton(Substitute.For<IDialogService>());
    }

    [Fact]
    public void LayoutRendersHeaderWithCorrectFont()
    {
        RenderFragment body = builder => builder.AddMarkupContent(0, "<p>child</p>");
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, body));
        var header = cut.Find("h5.mud-typography-h5");
        Assert.Equal("Predictotronix", header.TextContent.Trim());
    }

    [Fact]
    public void LayoutRendersSubscribeButton()
    {
        RenderFragment body = builder => builder.AddMarkupContent(0, "<p>child</p>");
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, body));
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b => b.TextContent.Trim() == "Subscribe");
    }
}
