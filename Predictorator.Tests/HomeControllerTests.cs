using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Predictorator.Data;
using System.Collections.Generic;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class HomeControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HomeControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
                services.AddSingleton<IDateRangeCalculator, DateRangeCalculator>();
                services.AddSingleton<IDateTimeProvider>(new SystemDateTimeProvider());
                services.AddSingleton<IRateLimitService>(sp =>
                    new InMemoryRateLimitService(100, TimeSpan.FromMinutes(1), sp.GetRequiredService<IDateTimeProvider>()));

                services.AddTransient<IFixtureService>(_ => new FakeFixtureService(
                    new FixturesResponse
                    {
                        Response = new List<FixtureData>
                        {
                            new()
                            {
                                Fixture = new Fixture { Date = DateTime.UtcNow, Venue = new Venue { Name="A", City="B" } },
                                Teams = new Teams { Home = new Team { Name="Home", Logo="" }, Away = new Team { Name="Away", Logo="" } },
                                Score = new Score { Fulltime = new ScoreHomeAway { Home = null, Away = null } }
                            }
                        }
                    }));
            });
        });
    }

    [Fact]
    public async Task Index_returns_view_with_buttons()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        Assert.NotNull(doc.QuerySelector("#copyBtn"));
        Assert.NotNull(doc.QuerySelector("#fillRandomBtn"));
        Assert.NotNull(doc.QuerySelector("#clearBtn"));
    }
}
