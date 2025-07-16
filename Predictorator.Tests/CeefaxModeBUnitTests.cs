using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Predictorator.Components;
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
        var browser = new BrowserInteropService(jsRuntime);
        ctx.Services.AddSingleton(browser);
        var theme = new ThemeService(browser);
        ctx.Services.AddSingleton(theme);
        ctx.Services.AddScoped<ToastInterop>();
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
        var toggle = cut.Find("#ceefaxToggle");
        Assert.False(ctx.Services.GetRequiredService<ThemeService>().IsCeefax);
        toggle.Click();
        cut.WaitForAssertion(() =>
        {
            Assert.True(ctx.Services.GetRequiredService<ThemeService>().IsCeefax);
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
        jsRuntime.InvokeAsync<string?>("app.getLocalStorage", Arg.Any<object[]>()).Returns("true");
        var browser = new BrowserInteropService(jsRuntime);
        ctx.Services.AddSingleton(browser);
        var theme = new ThemeService(browser);
        ctx.Services.AddSingleton(theme);
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

        cut.WaitForAssertion(() =>
        {
            Assert.True(ctx.Services.GetRequiredService<ThemeService>().IsCeefax);
        }, timeout: TimeSpan.FromSeconds(1));
    }
}
