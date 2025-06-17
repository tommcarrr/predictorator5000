using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Predictorator.UiTests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class HomePageTests : PageTest
{
    private string BaseUrl => TestContext.Parameters.Get("BaseUrl", "http://localhost:5000");

    [SetUp]
    public void CheckRunFlag()
    {
        if (Environment.GetEnvironmentVariable("RUN_UI_TESTS") != "true")
        {
            Assert.Ignore("UI tests are disabled");
        }
    }

    [Test]
    public async Task Index_Should_Display_Title()
    {
        await Page.GotoAsync(BaseUrl);
        var header = Page.GetByRole(AriaRole.Heading, new() { Name = "Premier League Fixtures" });
        await Expect(header).ToBeVisibleAsync();
    }
}
