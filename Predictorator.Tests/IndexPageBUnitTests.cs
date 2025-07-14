using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;
using NSubstitute;
using Predictorator.Components.Layout;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;
using IndexPage = Predictorator.Components.Pages.Index;

namespace Predictorator.Tests;

public class IndexPageBUnitTests
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
        return ctx;
    }

    [Fact]
    public async Task PageContainsDateRangePicker()
    {
        await using var ctx = CreateContext();
        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.CloseComponent();
        };
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var picker = cut.Find("#dateRangePicker");
        Assert.NotNull(picker);
    }

    [Fact]
    public async Task FixtureRow_Should_Render_With_Alignment_Classes()
    {
        await using var ctx = CreateContext();
        var fixtures = new FixturesResponse
        {
            Response = new List<FixtureData>
            {
                new()
                {
                    Fixture = new Fixture { Date = DateTime.UtcNow, Venue = new Venue { Name="A", City="B" } },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Home", Logo = string.Empty },
                        Away = new Team { Name = "Away", Logo = string.Empty }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = null, Away = null } }
                }
            }
        };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));

        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.CloseComponent();
        };

        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));

        var home = cut.Find(".fixture-line .home-name");
        var away = cut.Find(".fixture-line .away-name");
        Assert.NotNull(home);
        Assert.NotNull(away);
    }

    [Fact]
    public async Task WeekNavigation_Uses_WeekOffset_And_Buttons_Remain_Enabled()
    {
        await using var ctx = CreateContext();
        var navMan = (NavigationManager)ctx.Services.GetRequiredService<NavigationManager>();
        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.CloseComponent();
        };
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var next = cut.Find("#nextWeekBtn");
        var prev = cut.Find("#prevWeekBtn");
        Assert.False(next.HasAttribute("disabled"));
        Assert.False(prev.HasAttribute("disabled"));

        next.Click();
        Assert.Contains("weekOffset=1", navMan.Uri);
        Assert.DoesNotContain("fromDate", navMan.Uri);
        Assert.DoesNotContain("toDate", navMan.Uri);

        cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        next = cut.Find("#nextWeekBtn");
        prev = cut.Find("#prevWeekBtn");
        Assert.False(next.HasAttribute("disabled"));
        Assert.False(prev.HasAttribute("disabled"));
    }

    [Fact]
    public async Task ResetButton_Removes_Date_Range_And_Reenables_Week_Navigation()
    {
        await using var ctx = CreateContext();
        var navMan = (NavigationManager)ctx.Services.GetRequiredService<NavigationManager>();
        navMan.NavigateTo("/?fromDate=2024-02-01&toDate=2024-02-07");

        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.CloseComponent();
        };

        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var next = cut.Find("#nextWeekBtn");
        var reset = cut.Find("#resetBtn");
        Assert.True(next.HasAttribute("disabled"));
        Assert.False(reset.HasAttribute("disabled"));

        reset.Click();
        Assert.DoesNotContain("fromDate", navMan.Uri);
        Assert.DoesNotContain("toDate", navMan.Uri);

        cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        next = cut.Find("#nextWeekBtn");
        Assert.False(next.HasAttribute("disabled"));
    }
}
