using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using AngleSharp.Dom;
using Predictorator.Components.Layout;
using Predictorator.Tests.Helpers;
using Predictorator.Components.Pages.Subscription;
using Predictorator.Services;

namespace Predictorator.Tests;

public class MainLayoutBUnitTests
{
    private BunitContext CreateContext(bool enableEmail = true, bool enableSms = true)
    {
        var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var jsRuntime = Substitute.For<IJSRuntime>();
        ctx.Services.AddSingleton(jsRuntime);
        var storage = new FakeBrowserStorage();
        ctx.Services.AddSingleton<IBrowserStorage>(storage);
        ctx.Services.AddScoped<ToastInterop>();
        ctx.Services.AddScoped<UiModeService>();
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());

        var settings = new Dictionary<string, string?>();
        if (enableEmail)
            settings["Resend:ApiToken"] = "token";
        if (enableSms)
        {
            settings["Twilio:AccountSid"] = "sid";
            settings["Twilio:AuthToken"] = "token";
            settings["Twilio:FromNumber"] = "+1";
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ctx.Services.AddSingleton<IConfiguration>(config);
        ctx.Services.AddSingleton<NotificationFeatureService>();
        return ctx;
    }

    [Fact]
    public async Task LayoutRendersHeaderWithCorrectFont()
    {
        await using var ctx = CreateContext();
        var storage = (FakeBrowserStorage)ctx.Services.GetRequiredService<IBrowserStorage>();
        await storage.SetAsync("ceefaxMode", false);
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
        if (!buttons.Any(b => b.TextContent.Trim() == "Subscribe"))
        {
            cut.Find("#menuToggle").Click();
            Assert.NotNull(cut.Find("#subscribeButton"));
        }
        else
        {
            Assert.Contains(buttons, b => b.TextContent.Trim() == "Subscribe");
        }
    }

    [Fact]
    public async Task LayoutRendersSupportLink()
    {
        await using var ctx = CreateContext();
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        IElement link;
        try
        {
            link = cut.Find("#donateLink");
        }
        catch (ElementNotFoundException)
        {
            cut.Find("#menuToggle").Click();
            link = cut.Find("#donateLink");
        }
        Assert.NotNull(link);
        Assert.Equal("_blank", link.GetAttribute("target"));
    }

    [Fact]
    public async Task DarkModeToggleUpdatesState()
    {
        await using var ctx = CreateContext();
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        IElement toggle;
        try
        {
            toggle = cut.Find("#darkModeToggle");
        }
        catch (ElementNotFoundException)
        {
            cut.Find("#menuToggle").Click();
            toggle = cut.Find("#darkModeToggle");
        }
        toggle.Click();
        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.Instance.IsDarkMode);
        });
    }

    [Fact]
    public async Task SubscribeButtonOpensDialog()
    {
        await using var ctx = CreateContext();
        var dialog = ctx.Services.GetRequiredService<IDialogService>();
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var buttons = cut.FindAll("button");
        IElement button;
        if (!buttons.Any(b => b.TextContent.Trim() == "Subscribe"))
        {
            cut.Find("#menuToggle").Click();
            button = cut.Find("#subscribeButton");
        }
        else
        {
            button = buttons.First(b => b.TextContent.Trim() == "Subscribe");
        }
        button.Click();
        dialog.Received().Show<Subscribe>(Arg.Any<string>());
    }

    [Fact]
    public async Task LayoutRegistersToastHandler()
    {
        await using var ctx = CreateContext();
        var js = ctx.Services.GetRequiredService<IJSRuntime>();
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        cut.WaitForAssertion(() =>
        {
            js.Received().InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>("app.registerToastHandler", Arg.Any<object[]>());
        });
    }

    [Fact]
    public async Task SubscribeButtonNotRendered_When_Features_Disabled()
    {
        await using var ctx = CreateContext(enableEmail: false, enableSms: false);
        RenderFragment body = b => b.AddMarkupContent(0, "<p>child</p>");
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Subscribe");
    }
}
