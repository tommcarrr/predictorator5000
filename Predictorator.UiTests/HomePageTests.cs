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

        var context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 375, Height = 667 },
            UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1",
            HasTouch = true
        });
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

    [Test]
    public async Task TripleTapTeamName_Should_StartPongGame()
    {
        await NavigateWithRetriesAsync(_page!, BaseUrl);
        await _page!.EvaluateAsync(@"() => {
            document.querySelector('.home-name').textContent = 'Aston Villa';
            document.querySelector('.away-name').textContent = 'Newcastle';
        }");
        var teamName = "Aston Villa";
        await _page!.EvaluateAsync(@"() => {
            const el = document.querySelector('.team-name');
            const tap = () => {
                const touch = new Touch({ identifier: Date.now(), target: el, clientX: 0, clientY: 0 });
                el.dispatchEvent(new TouchEvent('touchstart', { touches: [touch], bubbles: true, cancelable: true }));
            };
            tap();
            setTimeout(tap, 100);
            setTimeout(tap, 200);
        }");
        await _page!.WaitForSelectorAsync("#pongOverlay");
        await _page!.WaitForSelectorAsync("#pongScore");
        await _page!.WaitForSelectorAsync("#pongTimer");
        var scoreText = await _page!.TextContentAsync("#pongScore");
        StringAssert.Contains(teamName, scoreText!);
        var timerText = await _page!.TextContentAsync("#pongTimer");
        Assert.That(int.Parse(timerText!), Is.InRange(0, 30));
        var prevId = await _page!.EvaluateAsync<string>("document.querySelector('#pongTimer').previousElementSibling.id");
        Assert.That(prevId, Is.EqualTo("pongScore"));
        var playerFirst = await _page!.EvaluateAsync<string>("document.querySelector('#pongPlayerName').children[0].style.color");
        var playerSecond = await _page!.EvaluateAsync<string>("document.querySelector('#pongPlayerName').children[1].style.color");
        Assert.That(playerFirst, Is.Not.EqualTo(playerSecond));
        var compFirst = await _page!.EvaluateAsync<string>("document.querySelector('#pongComputerName').children[0].style.color");
        var compSecond = await _page!.EvaluateAsync<string>("document.querySelector('#pongComputerName').children[1].style.color");
        Assert.That(compFirst, Is.Not.EqualTo(compSecond));
    }
}
