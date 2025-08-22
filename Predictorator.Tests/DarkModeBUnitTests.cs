using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using AngleSharp.Dom;
using Predictorator.Components;
using Predictorator.Components.Layout;
using Predictorator.Core.Models.Fixtures;
using Predictorator.Services;
using ThemeStyles = Predictorator.Themes.Themes;
using Predictorator.Core.Services;
using Predictorator.Tests.Helpers;
using Predictorator.Core.Data;

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
        ctx.Services.AddScoped<UiModeService>();
        var fixtures = new FixturesResponse { Response = [] };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));
        ctx.Services.AddSingleton<IGameWeekService>(new FakeGameWeekService());
        var provider = new FakeDateTimeProvider
        {
            Today = new DateTime(2024, 1, 1),
            UtcNow = new DateTime(2024, 1, 1)
        };
        ctx.Services.AddSingleton<IDateTimeProvider>(provider);
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
        ctx.Services.AddSingleton<IAnnouncementRepository, InMemoryAnnouncementRepository>();
        ctx.Services.AddSingleton<AnnouncementService>();
        return ctx;
    }

    [Fact]
    public async Task ToggleDarkMode_UpdatesServiceState()
    {
        await using var ctx = CreateContext();
        var cut = ctx.Render<App>();
        var layout = cut.FindComponent<MainLayout>();
        var service = ctx.Services.GetRequiredService<UiModeService>();
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
        Assert.True(service.IsDarkMode);

        toggle.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.False(service.IsDarkMode);
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
        ctx.Services.AddScoped<UiModeService>();
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(new FixturesResponse { Response = [] }));
        ctx.Services.AddSingleton<IGameWeekService>(new FakeGameWeekService());
        var provider2 = new FakeDateTimeProvider { Today = new DateTime(2024, 1, 1), UtcNow = new DateTime(2024, 1, 1) };
        ctx.Services.AddSingleton<IDateTimeProvider>(provider2);
        ctx.Services.AddSingleton<IDateRangeCalculator>(new DateRangeCalculator(provider2));

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
        ctx.Services.AddSingleton<IAnnouncementRepository, InMemoryAnnouncementRepository>();
        ctx.Services.AddSingleton<AnnouncementService>();

        var cut = ctx.Render<App>();
        var layout = cut.FindComponent<MainLayout>();
        var service = ctx.Services.GetRequiredService<UiModeService>();

        cut.WaitForAssertion(() =>
        {
            Assert.True(service.IsDarkMode);
        }, timeout: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void FootballTheme_Defines_DarkPalette()
    {
        Assert.Equal("#121212", ThemeStyles.FootballPredictorTheme.PaletteDark!.Background);
        Assert.Equal("#1E5F3E", ThemeStyles.FootballPredictorTheme.PaletteDark.Primary);
    }
}
