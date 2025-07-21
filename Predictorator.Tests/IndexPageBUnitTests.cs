using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MudBlazor.Services;
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
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var storage = new FakeBrowserStorage();
        ctx.Services.AddSingleton<IBrowserStorage>(storage);
        ctx.Services.AddScoped<ToastInterop>();
        ctx.Services.AddScoped<UiModeService>();
        var fixtures = new FixturesResponse { Response = [] };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));
        var gwService = new FakeGameWeekService();
        gwService.Items.AddRange([
            new Predictorator.Models.GameWeek { Season = "24-25", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) },
            new Predictorator.Models.GameWeek { Season = "24-25", Number = 2, StartDate = DateTime.Today.AddDays(7), EndDate = DateTime.Today.AddDays(13) }
        ]);
        ctx.Services.AddSingleton<IGameWeekService>(gwService);
        var provider = new FakeDateTimeProvider
        {
            Today = new DateTime(2024, 1, 1),
            UtcNow = new DateTime(2024, 1, 1)
        };
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
    public async Task PageContainsDateRangePicker()
    {
        await using var ctx = CreateContext();
        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
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
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
            b.CloseComponent();
        };

        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));

        var row = cut.Find("[data-testid=fixture-row]");
        var inputs = cut.FindAll("[data-testid=score-input]");
        Assert.NotNull(row);
        Assert.Equal(2, inputs.Count);
    }

    [Fact]
    public async Task GameWeekNavigation_Uses_GameWeek_Route_And_Buttons_Remain_Enabled()
    {
        await using var ctx = CreateContext();
        var navMan = (NavigationManager)ctx.Services.GetRequiredService<NavigationManager>();
        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
            b.CloseComponent();
        };
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var next = cut.Find("#nextWeekBtn");
        var prev = cut.Find("#prevWeekBtn");
        Assert.False(next.HasAttribute("disabled"));
        Assert.False(prev.HasAttribute("disabled"));

        next.Click();
        Assert.Contains("/24-25/gw2", navMan.Uri);
        Assert.DoesNotContain("fromDate", navMan.Uri);
        Assert.DoesNotContain("toDate", navMan.Uri);

        cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        next = cut.Find("#nextWeekBtn");
        prev = cut.Find("#prevWeekBtn");
        Assert.False(next.HasAttribute("disabled"));
        Assert.False(prev.HasAttribute("disabled"));
    }

    [Fact]
    public async Task CopyButton_Has_Expected_Id_And_No_OnClick()
    {
        await using var ctx = CreateContext();
        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.CloseComponent();
        };
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        var btn = cut.Find("#copyBtn");
        Assert.False(btn.HasAttribute("onclick"));
    }

    [Fact]
    public async Task RootPath_Navigates_To_Next_GameWeek()
    {
        await using var ctx = CreateContext();
        var navMan = (NavigationManager)ctx.Services.GetRequiredService<NavigationManager>();
        RenderFragment body = b => { b.OpenComponent<IndexPage>(0); b.CloseComponent(); };
        ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));

        Assert.Contains("/24-25/gw1", navMan.Uri);
    }

}
