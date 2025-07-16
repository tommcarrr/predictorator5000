using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Predictorator.Components;
using Predictorator.Components.Layout;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class DarkModeBUnitTests
{
    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var storage = new FakeBrowserStorage();
        ctx.Services.AddSingleton<IBrowserStorage>(storage);
        ctx.Services.AddScoped<ToastInterop>();
        var fixtures = new FixturesResponse { Response = [] };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));
        var provider = new FakeDateTimeProvider
        {
            Today = new DateTime(2024, 1, 1),
            UtcNow = new DateTime(2024, 1, 1)
        };
        ctx.Services.AddSingleton<IDateRangeCalculator>(new DateRangeCalculator(provider));
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());

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
    public async Task ToggleDarkMode_UpdatesServiceState()
    {
        await using var ctx = CreateContext();
        var cut = ctx.Render<App>();
        var layout = cut.FindComponent<MainLayout>();
        var toggle = cut.Find("#darkModeToggle");
        Assert.False(layout.Instance.IsDarkMode);

        toggle.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(layout.Instance.IsDarkMode);
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Initialize_Uses_Existing_State()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var storage = new FakeBrowserStorage();
        await storage.SetAsync("darkMode", true);
        ctx.Services.AddSingleton<IBrowserStorage>(storage);
        ctx.Services.AddScoped<ToastInterop>();
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

        cut.WaitForAssertion(() =>
        {
            Assert.True(layout.Instance.IsDarkMode);
        }, timeout: TimeSpan.FromSeconds(1));
    }
}
