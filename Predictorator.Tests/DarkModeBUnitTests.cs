using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Microsoft.EntityFrameworkCore;
using Resend;
using Hangfire;
using Predictorator.Data;
using Predictorator.Components;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class DarkModeBUnitTests
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
        var theme = new ThemeService(browser);
        ctx.Services.AddSingleton(theme);
        var fixtures = new FixturesResponse { Response = [] };
        var fixtureService = new FakeFixtureService(fixtures);
        ctx.Services.AddSingleton<IFixtureService>(fixtureService);
        var provider = new FakeDateTimeProvider
        {
            Today = new DateTime(2024, 1, 1),
            UtcNow = new DateTime(2024, 1, 1)
        };
        var calculator = new DateRangeCalculator(provider);
        ctx.Services.AddSingleton<IDateRangeCalculator>(calculator);
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
        var features = new NotificationFeatureService(config);
        ctx.Services.AddSingleton(features);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var resend = Substitute.For<IResend>();
        var sms = Substitute.For<ITwilioSmsSender>();
        var jobs = Substitute.For<IBackgroundJobClient>();
        var time = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow, Today = DateTime.Today };
        ctx.Services.AddSingleton(new NotificationService(db, resend, sms, config, fixtureService, calculator, features, time, jobs));
        return ctx;
    }

    [Fact]
    public async Task ToggleDarkMode_UpdatesServiceState()
    {
        await using var ctx = CreateContext();
        var cut = ctx.Render<App>();
        var toggle = cut.Find("#darkModeToggle");
        Assert.False(ctx.Services.GetRequiredService<ThemeService>().IsDarkMode);

        toggle.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(ctx.Services.GetRequiredService<ThemeService>().IsDarkMode);
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
        jsRuntime.InvokeAsync<string?>("app.getLocalStorage", Arg.Any<object[]>())
            .Returns("true");
        var browser = new BrowserInteropService(jsRuntime);
        ctx.Services.AddSingleton(browser);
        var theme = new ThemeService(browser);
        ctx.Services.AddSingleton(theme);
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        var fixtureService2 = new FakeFixtureService(new FixturesResponse { Response = [] });
        ctx.Services.AddSingleton<IFixtureService>(fixtureService2);
        var provider2 = new FakeDateTimeProvider { Today = new DateTime(2024, 1, 1), UtcNow = new DateTime(2024, 1, 1) };
        var calculator2 = new DateRangeCalculator(provider2);
        ctx.Services.AddSingleton<IDateRangeCalculator>(calculator2);

        var settings = new Dictionary<string, string?>
        {
            ["Resend:ApiToken"] = "token",
            ["Twilio:AccountSid"] = "sid",
            ["Twilio:AuthToken"] = "token",
            ["Twilio:FromNumber"] = "+1"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ctx.Services.AddSingleton<IConfiguration>(config);
        var features2 = new NotificationFeatureService(config);
        ctx.Services.AddSingleton(features2);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var resend = Substitute.For<IResend>();
        var sms = Substitute.For<ITwilioSmsSender>();
        var jobs = Substitute.For<IBackgroundJobClient>();
        var time = new FakeDateTimeProvider { UtcNow = DateTime.UtcNow, Today = DateTime.Today };
        ctx.Services.AddSingleton(new NotificationService(db, resend, sms, config, fixtureService2, calculator2, features2, time, jobs));

        var cut = ctx.Render<App>();

        cut.WaitForAssertion(() =>
        {
            Assert.True(ctx.Services.GetRequiredService<ThemeService>().IsDarkMode);
        }, timeout: TimeSpan.FromSeconds(1));
    }
}
