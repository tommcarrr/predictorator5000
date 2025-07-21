using System.Threading.RateLimiting;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Predictorator.Data;
using Predictorator.Models.Fixtures;
using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class HomePageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HomePageTests(WebApplicationFactory<Program> factory)
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
                services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                    });
                });

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

                var gwService = new FakeGameWeekService();
                gwService.Items.Add(new Predictorator.Models.GameWeek { Season = "24-25", Number = 1, StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(6) });
                services.AddSingleton<IGameWeekService>(gwService);

                services.RemoveAll(typeof(IBrowserStorage));
                services.AddSingleton<IBrowserStorage>(new FakeBrowserStorage());
                services.AddScoped<UiModeService>();
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
