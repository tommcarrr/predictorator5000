using Bunit;
using Microsoft.AspNetCore.Http;
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

public class DarkModeBUnitTests
{
    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<bool>("app.getDarkMode", Arg.Any<object[]?>()).Returns(new ValueTask<bool>(false));
        ctx.Services.AddSingleton(jsRuntime);
        var browser = new BrowserInteropService(jsRuntime);
        ctx.Services.AddSingleton(browser);
        var theme = new ThemeService(browser);
        theme.InitializeAsync().GetAwaiter().GetResult();
        ctx.Services.AddSingleton(theme);
        var fixtures = new FixturesResponse { Response = [] };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));
        var provider = new FakeDateTimeProvider
        {
            Today = new DateTime(2024, 1, 1),
            UtcNow = new DateTime(2024, 1, 1)
        };
        ctx.Services.AddSingleton<IDateRangeCalculator>(new DateRangeCalculator(provider));
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        return ctx;
    }

    [Fact]
    public async Task ToggleDarkModeAppliesDarkClass()
    {
        await using var ctx = CreateContext();
        var cut = ctx.Render<App>();
        var toggle = cut.Find("#darkModeToggle");
        Assert.False(ctx.Services.GetRequiredService<ThemeService>().IsDarkMode);

        toggle.Click();

        cut.WaitForAssertion(() => Assert.True(ctx.Services.GetRequiredService<ThemeService>().IsDarkMode), timeout: TimeSpan.FromSeconds(1));
    }
}
