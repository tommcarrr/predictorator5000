using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Microsoft.JSInterop;
using AngleSharp.Dom;
using Predictorator.Components;
using Predictorator.Components.Layout;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class CeefaxModeBUnitTests
{
    private BunitContext CreateContext()
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
        var fixtures = new FixturesResponse { Response = [] };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));
        var provider = new FakeDateTimeProvider { Today = new DateTime(2024, 1, 1), UtcNow = new DateTime(2024, 1, 1) };
        ctx.Services.AddSingleton<IDateRangeCalculator>(new DateRangeCalculator(provider));

        var settings = new Dictionary<string, string?>
        {
            ["Resend:ApiToken"] = "token",
            ["Twilio:AccountSid"] = "sid",
            ["Twilio:AuthToken"] = "token",
            ["Twilio:FromNumber"] = "+1"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ctx.Services.AddSingleton<IConfiguration>(config);
        ctx.Services.AddSingleton<NotificationFeatureService>();
        return ctx;
    }

    [Fact]
    public async Task ToggleCeefax_UpdatesServiceState()
    {
        await using var ctx = CreateContext();
        var cut = ctx.Render<App>();
        var layout = cut.FindComponent<MainLayout>();
        var service = ctx.Services.GetRequiredService<UiModeService>();
        IElement toggle;
        try
        {
            toggle = cut.Find("#ceefaxToggle");
        }
        catch (ElementNotFoundException)
        {
            cut.Find("#menuToggle").Click();
            toggle = cut.Find("#ceefaxToggle");
        }
        Assert.False(service.IsCeefax);
        toggle.Click();
        cut.WaitForAssertion(() =>
        {
            Assert.True(service.IsCeefax);
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ToggleCeefax_Enables_DarkMode()
    {
        await using var ctx = CreateContext();
        var cut = ctx.Render<App>();
        var service = ctx.Services.GetRequiredService<UiModeService>();
        IElement toggle;
        try
        {
            toggle = cut.Find("#ceefaxToggle");
        }
        catch (ElementNotFoundException)
        {
            cut.Find("#menuToggle").Click();
            toggle = cut.Find("#ceefaxToggle");
        }
        Assert.False(service.IsDarkMode);
        toggle.Click();
        cut.WaitForAssertion(() =>
        {
            Assert.True(service.IsDarkMode);
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CeefaxToggle_Uses_Dark_Color_When_Off()
    {
        await using var ctx = CreateContext();
        var cut = ctx.Render<App>();
        IElement toggle;
        try
        {
            toggle = cut.Find("#ceefaxToggle");
        }
        catch (ElementNotFoundException)
        {
            cut.Find("#menuToggle").Click();
            toggle = cut.Find("#ceefaxToggle");
        }
        Assert.Contains("mud-dark-text", toggle.ClassName);

        toggle.Click();
        cut.WaitForAssertion(() =>
        {
            try
            {
                cut.Find("#ceefaxToggle");
            }
            catch (ElementNotFoundException)
            {
                cut.Find("#menuToggle").Click();
            }
            var t = cut.Find("#ceefaxToggle");
            Assert.Contains("mud-inherit-text", t.ClassName);
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CeefaxToggle_Invokes_JsRuntime()
    {
        await using var ctx = CreateContext();
        var js = ctx.Services.GetRequiredService<IJSRuntime>();
        var cut = ctx.Render<App>();
        IElement toggle;
        try
        {
            toggle = cut.Find("#ceefaxToggle");
        }
        catch (ElementNotFoundException)
        {
            cut.Find("#menuToggle").Click();
            toggle = cut.Find("#ceefaxToggle");
        }
        toggle.Click();
        cut.WaitForAssertion(() =>
        {
            js.Received().InvokeVoidAsync("app.setCeefax", true);
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Initialize_Uses_Existing_State()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var jsRuntime = Substitute.For<IJSRuntime>();
        ctx.Services.AddSingleton(jsRuntime);
        var storage = new FakeBrowserStorage();
        await storage.SetAsync("ceefaxMode", true);
        ctx.Services.AddSingleton<IBrowserStorage>(storage);
        ctx.Services.AddScoped<ToastInterop>();
        ctx.Services.AddScoped<UiModeService>();
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(new FixturesResponse { Response = [] }));
        ctx.Services.AddSingleton<IDateRangeCalculator>(new DateRangeCalculator(new FakeDateTimeProvider { Today = new DateTime(2024, 1, 1), UtcNow = new DateTime(2024, 1, 1) }));

        var settings = new Dictionary<string, string?>
        {
            ["Resend:ApiToken"] = "token",
            ["Twilio:AccountSid"] = "sid",
            ["Twilio:AuthToken"] = "token",
            ["Twilio:FromNumber"] = "+1"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ctx.Services.AddSingleton<IConfiguration>(config);
        ctx.Services.AddSingleton<NotificationFeatureService>();

        var cut = ctx.Render<App>();
        var layout = cut.FindComponent<MainLayout>();
        var service = ctx.Services.GetRequiredService<UiModeService>();

        cut.WaitForAssertion(() =>
        {
            Assert.True(service.IsCeefax);
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Initialize_Ceefax_Enables_DarkMode()
    {
        await using var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var jsRuntime = Substitute.For<IJSRuntime>();
        ctx.Services.AddSingleton(jsRuntime);
        var storage = new FakeBrowserStorage();
        await storage.SetAsync("ceefaxMode", true);
        ctx.Services.AddSingleton<IBrowserStorage>(storage);
        ctx.Services.AddScoped<ToastInterop>();
        ctx.Services.AddScoped<UiModeService>();
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(new FixturesResponse { Response = [] }));
        ctx.Services.AddSingleton<IDateRangeCalculator>(new DateRangeCalculator(new FakeDateTimeProvider { Today = new DateTime(2024, 1, 1), UtcNow = new DateTime(2024, 1, 1) }));

        var settings = new Dictionary<string, string?>
        {
            ["Resend:ApiToken"] = "token",
            ["Twilio:AccountSid"] = "sid",
            ["Twilio:AuthToken"] = "token",
            ["Twilio:FromNumber"] = "+1"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ctx.Services.AddSingleton<IConfiguration>(config);
        ctx.Services.AddSingleton<NotificationFeatureService>();

        var cut = ctx.Render<App>();
        var layout = cut.FindComponent<MainLayout>();
        var service = ctx.Services.GetRequiredService<UiModeService>();

        cut.WaitForAssertion(() =>
        {
            Assert.True(service.IsDarkMode);
        }, timeout: TimeSpan.FromSeconds(1));
    }
}
