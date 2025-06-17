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
        var context = await _browser.NewContextAsync();
        _page = await context.NewPageAsync();
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
        var header = await _page.TextContentAsync("h1");
        Assert.That(header, Is.EqualTo("Premier League Fixtures"));
    }
}
