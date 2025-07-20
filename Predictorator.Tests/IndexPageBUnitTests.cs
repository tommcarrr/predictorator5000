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

        var row = cut.Find("[data-testid=fixture-row]");
        var inputs = cut.FindAll("[data-testid=score-input]");
        Assert.NotNull(row);
        Assert.Equal(2, inputs.Count);
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
    public async Task AutoWeek_Increments_Offset_When_No_Fixtures()
    {
        await using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        ctx.Services.AddSingleton<IBrowserStorage>(new FakeBrowserStorage());
        ctx.Services.AddScoped<ToastInterop>();
        ctx.Services.AddScoped<UiModeService>();

        var provider = new FakeDateTimeProvider
        {
            Today = new DateTime(2024, 1, 1),
            UtcNow = new DateTime(2024, 1, 1)
        };
        ctx.Services.AddSingleton<IDateRangeCalculator>(new DateRangeCalculator(provider));
        var fixtureService = new AutoWeekFixtureService();
        ctx.Services.AddSingleton<IFixtureService>(fixtureService);

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

        var navMan = (NavigationManager)ctx.Services.GetRequiredService<NavigationManager>();
        RenderFragment body = b => { b.OpenComponent<IndexPage>(0); b.CloseComponent(); };
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));

        cut.WaitForAssertion(() =>
        {
            Assert.True(fixtureService.RequestedFromDates.Count >= 2);
            Assert.Equal(new DateTime(2023, 12, 29), fixtureService.RequestedFromDates[0]);
            Assert.Equal(new DateTime(2024, 1, 5), fixtureService.RequestedFromDates[1]);
            cut.Find("[data-testid=fixture-row]");
        }, timeout: TimeSpan.FromSeconds(1));

        cut.Find("#nextWeekBtn").Click();
        Assert.Contains("weekOffset=2", navMan.Uri);
    }

    private class AutoWeekFixtureService : IFixtureService
    {
        public List<DateTime> RequestedFromDates { get; } = new();

        public Task<FixturesResponse> GetFixturesAsync(DateTime fromDate, DateTime toDate)
        {
            RequestedFromDates.Add(fromDate.Date);
            if (fromDate.Date == new DateTime(2023, 12, 29))
            {
                return Task.FromResult(new FixturesResponse { FromDate = fromDate, ToDate = toDate });
            }

            return Task.FromResult(new FixturesResponse
            {
                FromDate = fromDate,
                ToDate = toDate,
                Response = new List<FixtureData>
                {
                    new()
                    {
                        Fixture = new Fixture { Id = 1, Date = fromDate, Venue = new Venue { Name = "A", City = "B" } },
                        Teams = new Teams { Home = new Team { Name = "Home", Logo = string.Empty }, Away = new Team { Name = "Away", Logo = string.Empty } },
                        Score = new Score { Fulltime = new ScoreHomeAway() }
                    }
                }
            });
        }
    }

}
