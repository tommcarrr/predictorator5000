using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Http;
using IndexPage = Predictorator.Components.Pages.Index;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;
using MudBlazor.Services;
using NSubstitute;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Predictorator.Components.Layout;
using Xunit;

namespace Predictorator.Tests;

public class IndexPageBUnitTests
{
    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        ctx.Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        var jsRuntime = Substitute.For<IJSRuntime>();
        ctx.Services.AddSingleton<IJSRuntime>(jsRuntime);
        ctx.Services.AddSingleton(new BrowserInteropService(jsRuntime));
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
}
