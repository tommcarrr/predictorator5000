using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Http;
using Predictorator.Components.Layout;
using Predictorator.Services;
using MudBlazor.Services;
using MudBlazor;
using NSubstitute;
using Xunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Predictorator.Models.Fixtures;
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
        ctx.Services.AddSingleton<IJSRuntime>(jsRuntime);
        var browser = new BrowserInteropService(jsRuntime);
        ctx.Services.AddSingleton(browser);
        var theme = new ThemeService(browser);
        theme.InitializeAsync().GetAwaiter().GetResult();
        ctx.Services.AddSingleton(theme);
        ctx.Services.AddSingleton(Substitute.For<IDialogService>());
        var fixtures = new FixturesResponse { Response = [] };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));
        var provider = new FakeDateTimeProvider
        {
            Today = new DateTime(2024, 1, 1),
            UtcNow = new DateTime(2024, 1, 1)
        };
        ctx.Services.AddSingleton<IDateRangeCalculator>(new DateRangeCalculator(provider));
        return ctx;
    }

    private class TestHost : ComponentBase
    {
        [Inject]
        public ThemeService ThemeService { get; set; } = null!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudThemeProvider>(0);
            builder.AddAttribute(1, "IsDarkMode", ThemeService.IsDarkMode);
            builder.AddAttribute(2, "IsDarkModeChanged", EventCallback.Factory.Create<bool>(this, ThemeService.SetDarkModeAsync));
            builder.CloseComponent();

            builder.OpenComponent<MainLayout>(3);
            builder.AddAttribute(4, "Body", (RenderFragment)(b => b.AddMarkupContent(0, "<p>content</p>")));
            builder.CloseComponent();
        }
    }

    [Fact]
    public async Task ToggleDarkModeAppliesDarkClass()
    {
        await using var ctx = CreateContext();
        var cut = ctx.Render<TestHost>();
        var theme = ctx.Services.GetRequiredService<ThemeService>();
        Assert.False(theme.IsDarkMode);

        var toggle = cut.Find("#darkModeToggle");
        toggle.Click();

        cut.WaitForAssertion(() => Assert.True(theme.IsDarkMode), timeout: System.TimeSpan.FromSeconds(1));
    }
}
