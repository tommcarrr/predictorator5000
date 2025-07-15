using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MudBlazor.Services;
using NSubstitute;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Resend;
using Hangfire;
using Predictorator.Components.Layout;
using Predictorator.Data;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;
using IndexPage = Predictorator.Components.Pages.Index;

namespace Predictorator.Tests;

public class IndexPageBUnitTests
{
    private BunitContext CreateContext(bool isAdmin = false)
    {
        var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        var accessor = new HttpContextAccessor();
        if (isAdmin)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") }, "Test");
            accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        }
        ctx.Services.AddSingleton<IHttpContextAccessor>(accessor);
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

    [Fact]
    public async Task AdminButtons_Not_Visible_For_Non_Admin()
    {
        await using var ctx = CreateContext();
        RenderFragment body = b => { b.OpenComponent<IndexPage>(0); b.CloseComponent(); };
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.Id == "sendNewFixturesBtn");
        Assert.DoesNotContain(cut.FindAll("button"), b => b.Id == "sendOneHourBtn");
    }

    [Fact]
    public async Task AdminButtons_Render_For_Admin()
    {
        await using var ctx = CreateContext(isAdmin: true);
        RenderFragment body = b => { b.OpenComponent<IndexPage>(0); b.CloseComponent(); };
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        Assert.NotNull(cut.Find("#sendNewFixturesBtn"));
        Assert.NotNull(cut.Find("#sendOneHourBtn"));
    }
}
