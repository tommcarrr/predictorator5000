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
using System.Security.Claims;
using System.Linq;

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
            ["Twilio:FromNumber"] = "+1",
            ["PredictionEmail:SpecialRecipients:0"] = "vip@example.com"
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
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
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

    [Fact]
    public async Task EmailButton_Displayed_For_Admin_User()
    {
        await using var ctx = CreateContext();
        var accessor = ctx.Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };

        RenderFragment body = b => { b.OpenComponent<IndexPage>(0); b.CloseComponent(); };
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));

        var btn = cut.Find("#emailBtn");
        Assert.NotNull(btn);
    }

    [Fact]
    public async Task EmailButton_Hidden_For_Non_Admin()
    {
        await using var ctx = CreateContext();
        ctx.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        RenderFragment body = b => { b.OpenComponent<IndexPage>(0); b.CloseComponent(); };
        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));

        Assert.Empty(cut.FindAll("#emailBtn"));
    }

    [Fact]
    public async Task EmailButton_Shows_Error_When_Predictions_Missing()
    {
        await using var ctx = CreateContext();
        var fixtures = new FixturesResponse
        {
            Response =
            [
                new FixtureData
                {
                    Fixture = new Fixture { Id = 1, Date = DateTime.UtcNow, Venue = new Venue { Name = "V" } },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Home", Logo = string.Empty },
                        Away = new Team { Name = "Away", Logo = string.Empty }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway() }
                }
            ]
        };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));

        var accessor = ctx.Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };

        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
            b.CloseComponent();
        };

        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=score-input]")));
        cut.Find("#emailBtn").Click();

        cut.WaitForAssertion(() =>
        {
            var snackbar = cut.Find("div.mud-snackbar");
            Assert.Contains("fill in all score predictions", snackbar.TextContent);
        });
    }

    [Fact]
    public async Task EmailButton_Opens_Dialog_When_Predictions_Complete()
    {
        await using var ctx = CreateContext();
        var fixtures = new FixturesResponse
        {
            Response =
            [
                new FixtureData
                {
                    Fixture = new Fixture { Id = 1, Date = DateTime.UtcNow, Venue = new Venue { Name = "V" } },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Home", Logo = string.Empty },
                        Away = new Team { Name = "Away", Logo = string.Empty }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = 1, Away = 0 } }
                }
            ]
        };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));

        var accessor = ctx.Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };

        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
            b.CloseComponent();
        };

        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        cut.Find("#emailBtn").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("div.mud-dialog");
            cut.Find("input[type=email]");
        });
    }

    [Fact]
    public async Task EmailDialog_Shows_Blank_When_No_Email_Remembered()
    {
        await using var ctx = CreateContext();
        var fixtures = new FixturesResponse
        {
            Response =
            [
                new FixtureData
                {
                    Fixture = new Fixture { Id = 1, Date = DateTime.UtcNow, Venue = new Venue { Name = "V" } },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Home", Logo = string.Empty },
                        Away = new Team { Name = "Away", Logo = string.Empty }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = 1, Away = 0 } }
                }
            ]
        };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));

        ctx.JSInterop.Setup<string?>("localStorage.getItem", "predictionsEmail").SetResult("NULL");

        var accessor = ctx.Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };

        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
            b.CloseComponent();
        };

        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        cut.Find("#emailBtn").Click();

        cut.WaitForAssertion(() =>
        {
            var input = cut.Find("input[type=email]");
            Assert.True(string.IsNullOrEmpty(input.GetAttribute("value")));
            var checkbox = cut.Find("input[type=checkbox]");
            Assert.False(checkbox.HasAttribute("checked"));
        });
    }

    [Fact]
    public async Task EmailDialog_Shows_Remembered_Email_And_Is_Checked()
    {
        await using var ctx = CreateContext();
        var fixtures = new FixturesResponse
        {
            Response =
            [
                new FixtureData
                {
                    Fixture = new Fixture { Id = 1, Date = DateTime.UtcNow, Venue = new Venue { Name = "V" } },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Home", Logo = string.Empty },
                        Away = new Team { Name = "Away", Logo = string.Empty }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = 1, Away = 0 } }
                }
            ]
        };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));

        ctx.JSInterop.Setup<string?>("localStorage.getItem", "predictionsEmail").SetResult("user@example.com");

        var accessor = ctx.Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };

        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
            b.CloseComponent();
        };

        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        cut.Find("#emailBtn").Click();

        cut.WaitForAssertion(() =>
        {
            var input = cut.Find("input[type=email]");
            Assert.Equal("user@example.com", input.GetAttribute("value"));
            var checkbox = cut.Find("input[type=checkbox]");
            Assert.True(checkbox.HasAttribute("checked"));
        });
    }

    [Fact]
    public async Task SubmitEmail_Sends_Mailto_And_Remembers_When_Checked()
    {
        await using var ctx = CreateContext();
        var navMan = new RecordingNavigationManager();
        ctx.Services.AddSingleton<NavigationManager>(navMan);

        var fixtures = new FixturesResponse
        {
            Response =
            [
                new FixtureData
                {
                    Fixture = new Fixture { Id = 1, Date = DateTime.UtcNow, Venue = new Venue { Name = "V" } },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Home", Logo = string.Empty },
                        Away = new Team { Name = "Away", Logo = string.Empty }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = 1, Away = 0 } }
                }
            ]
        };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));

        var accessor = ctx.Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };

        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
            b.CloseComponent();
        };

        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=score-input]")));
        cut.Find("#emailBtn").Click();

        cut.WaitForAssertion(() => cut.Find("input[type=email]"));
        cut.Find("input[type=email]").Change("user@example.com");
        cut.Find("input[type=checkbox]").Change(true);
        cut.FindAll("button").First(b => b.TextContent.Contains("Submit")).Click();

        Assert.Contains("mailto:user@example.com", navMan.NavigatedTo);

        var set = ctx.JSInterop.Invocations.Single(i => i.Identifier == "localStorage.setItem");
        Assert.Equal("predictionsEmail", set.Arguments[0]?.ToString());
        Assert.Equal("user@example.com", set.Arguments[1]?.ToString());
    }

    [Fact]
    public async Task SubmitEmail_Adds_Extra_Text_For_Configured_Email()
    {
        await using var ctx = CreateContext();
        var navMan = new RecordingNavigationManager();
        ctx.Services.AddSingleton<NavigationManager>(navMan);

        var fixtures = new FixturesResponse
        {
            Response =
            [
                new FixtureData
                {
                    Fixture = new Fixture { Id = 1, Date = DateTime.UtcNow, Venue = new Venue { Name = "V" } },
                    Teams = new Teams
                    {
                        Home = new Team { Name = "Home", Logo = string.Empty },
                        Away = new Team { Name = "Away", Logo = string.Empty }
                    },
                    Score = new Score { Fulltime = new ScoreHomeAway { Home = 1, Away = 0 } }
                }
            ]
        };
        ctx.Services.AddSingleton<IFixtureService>(new FakeFixtureService(fixtures));

        var accessor = ctx.Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test"))
        };

        RenderFragment body = b =>
        {
            b.OpenComponent<IndexPage>(0);
            b.AddAttribute(1, "Season", "24-25");
            b.AddAttribute(2, "Week", 1);
            b.CloseComponent();
        };

        var cut = ctx.Render<MainLayout>(p => p.Add(l => l.Body, body));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=score-input]")));
        cut.Find("#emailBtn").Click();

        cut.WaitForAssertion(() => cut.Find("input[type=email]"));
        cut.Find("input[type=email]").Change("vip@example.com");
        cut.FindAll("button").First(b => b.TextContent.Contains("Submit")).Click();

        var uri = navMan.NavigatedTo!;
        Assert.Contains("mailto:vip@example.com", uri);
        var bodyEncoded = uri.Split("&body=")[1];
        var bodyText = Uri.UnescapeDataString(bodyEncoded);
        Assert.StartsWith("Hello Helen Lyttle,", bodyText);
        var preIndex = bodyText.IndexOf("My predictions are as follows...");
        var tableIndex = bodyText.IndexOf("Home 1 - 0 Away");
        var postIndex = bodyText.IndexOf("Yours sincerely,");
        Assert.True(preIndex < tableIndex);
        Assert.True(tableIndex < postIndex);
        Assert.EndsWith("<insert name>.", bodyText.TrimEnd());
    }

    private class RecordingNavigationManager : NavigationManager
    {
        public string? NavigatedTo { get; private set; }

        public RecordingNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedTo = uri;
        }
    }

}
