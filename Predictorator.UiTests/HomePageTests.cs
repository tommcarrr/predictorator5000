using Microsoft.Playwright;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Predictorator.UiTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class HomePageTests
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    private string BaseUrl =>
        Environment.GetEnvironmentVariable("BASE_URL") ??
        TestContext.Parameters.Get("BaseUrl", "http://localhost:5000");

    [OneTimeSetUp]
    public async Task SetupAsync()
    {
        if (Environment.GetEnvironmentVariable("RUN_UI_TESTS") != "true")
        {
            Assert.Ignore("UI tests are disabled");
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });

        var headers = new Dictionary<string, string>();
        var token = Environment.GetEnvironmentVariable("UI_TEST_TOKEN");
        if (!string.IsNullOrEmpty(token))
        {
            headers["X-Test-Token"] = token;
        }

        var context = await _browser.NewContextAsync(new() { ExtraHTTPHeaders = headers });
        _page = await context.NewPageAsync();
        _page.SetDefaultTimeout(60000);
    }

    [OneTimeTearDown]
    public async Task TearDownAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
    }

    [Test]
    public async Task Index_Should_Display_Title()
    {
        await _page!.GotoAsync(BaseUrl);
        await _page.Locator("h1").WaitForAsync();
        var header = await _page.TextContentAsync("h1");
        Assert.That(header, Is.EqualTo("Premier League Fixtures"));
    }

    [Test]
    public async Task Index_Should_Display_Fixture_Row_When_Using_Mock_Data()
    {
        await _page!.GotoAsync(BaseUrl);
        await _page.Locator(".fixture-row").First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var rows = await _page.QuerySelectorAllAsync(".fixture-row");
        if (Environment.GetEnvironmentVariable("UI_TEST_TOKEN") != null)
        {
            Assert.IsNotEmpty(rows);
        }
        else
        {
            Assert.Pass("No test token provided; skipping row check.");
        }
    }

    [Test]
    public async Task SubscribePage_Should_Display_Form()
    {
        await _page!.GotoAsync($"{BaseUrl}/mvc/Subscription/Subscribe");
        await _page.Locator("h2").WaitForAsync();
        var header = await _page.TextContentAsync("h2");
        Assert.That(header, Is.EqualTo("Subscribe to Notifications"));
    }

    [Test]
    public async Task Admin_Route_Should_Display_Login_Page()
    {
        await _page!.GotoAsync($"{BaseUrl}/admin");
        await _page.Locator("h1").WaitForAsync();
        var header = await _page.TextContentAsync("h1");
        Assert.That(header, Does.Contain("Log in"));
    }
}
