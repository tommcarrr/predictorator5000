using Predictorator.Services;
using Predictorator.Tests.Helpers;

namespace Predictorator.Tests;

public class EmailCssInlinerTests
{
    [Fact]
    public void InlineCss_returns_input_when_file_missing()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        var env = new FakeWebHostEnvironment { WebRootPath = root };
        var inliner = new EmailCssInliner(env);
        var html = "<p>Hello</p>";

        var result = inliner.InlineCss(html);

        Assert.Equal(html, result);
    }

    [Fact]
    public void InlineCss_inlines_styles_from_css_file()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(root, "css"));
        File.WriteAllText(Path.Combine(root, "css", "email.css"), "p{color:red;}");
        var env = new FakeWebHostEnvironment { WebRootPath = root };
        var inliner = new EmailCssInliner(env);
        var html = "<p>Hello</p>";

        var result = inliner.InlineCss(html);
        var normalized = result.Replace(" ", "");

        Assert.Contains("style=\"color:red", normalized);
    }
}
