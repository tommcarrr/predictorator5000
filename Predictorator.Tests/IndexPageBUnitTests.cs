using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using IndexPage = Predictorator.Components.Pages.Index;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;
using MudBlazor.Services;
using NSubstitute;
using System.Linq;
using Xunit;

namespace Predictorator.Tests;

public class IndexPageBUnitTests
{
    private BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddMudServices();
        var pop = ctx.Services.FirstOrDefault(s => s.ServiceType.Name == "IPopoverService");
        if (pop != null) ctx.Services.Remove(pop);
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
    public void PageContainsDateRangePicker()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<IndexPage>();
        var picker = cut.Find("#dateRangePicker");
        Assert.NotNull(picker);
    }
}
