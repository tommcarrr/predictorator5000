using Microsoft.Playwright;

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

    private static async Task NavigateWithRetriesAsync(IPage page, string url, int maxAttempts = 3)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await page.GotoAsync(url, new() { Timeout = 90000 });
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                return;
            }
            catch (TimeoutException) when (attempt < maxAttempts)
            {
                await Task.Delay(5000);
            }
        }

        // Final attempt without catching to surface the exception
        await page.GotoAsync(url, new() { Timeout = 90000 });
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

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
        _page.SetDefaultTimeout(90000);
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
        await NavigateWithRetriesAsync(_page!, BaseUrl);
        await _page!.Locator("h1").WaitForAsync();
        var header = await _page!.TextContentAsync("h1");
        Assert.That(header, Is.EqualTo("Premier League Fixtures"));
    }

    [Test]
    public async Task SubscribePage_Should_Display_Form()
    {
        await NavigateWithRetriesAsync(_page!, $"{BaseUrl}/Subscription/Subscribe");
        await _page!.Locator("h2").WaitForAsync();
        var header = await _page!.TextContentAsync("h2");
        Assert.That(header, Is.EqualTo("Subscribe to Notifications"));
    }

    [Test]
    public async Task Login_Route_Should_Display_Login_Page()
    {
        await NavigateWithRetriesAsync(_page!, $"{BaseUrl}/login");
        await _page!.Locator("h1").WaitForAsync();
        var header = await _page!.TextContentAsync("h1");
        Assert.That(header, Does.Contain("Log in"));
    }

    [Test]
    public async Task WeekNavigationButtons_Should_Change_Url()
    {
        await NavigateWithRetriesAsync(_page!, BaseUrl);
        var initialUrl = _page!.Url;
        await _page.Locator("#nextWeekBtn").ClickAsync();
        await _page.WaitForURLAsync("**weekOffset=1**");
        StringAssert.Contains("weekOffset=1", _page.Url);
        await _page.Locator("#prevWeekBtn").ClickAsync();
        await _page.WaitForURLAsync(initialUrl);
        Assert.That(_page.Url, Is.EqualTo(initialUrl));
    }

    [Test]
    public async Task PrivacyPolicyLink_Should_Display_Dialog()
    {
        await NavigateWithRetriesAsync(_page!, BaseUrl);
        var initialUrl = _page!.Url;
        await _page.GetByRole(AriaRole.Link, new() { Name = "Privacy Policy" }).ClickAsync();
        await _page.GetByRole(AriaRole.Dialog).GetByText("Privacy Policy").WaitForAsync();
        Assert.That(_page.Url, Is.EqualTo(initialUrl));
    }
}
