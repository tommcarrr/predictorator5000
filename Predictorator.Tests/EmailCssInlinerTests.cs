using Predictorator.Core.Services;

namespace Predictorator.Tests;

public class EmailCssInlinerTests
{
    [Fact]
    public void InlineCss_returns_input_when_css_empty()
    {
        var inliner = new EmailCssInliner(string.Empty);
        var html = "<p>Hello</p>";

        var result = inliner.InlineCss(html);

        Assert.Equal(html, result);
    }

    [Fact]
    public void InlineCss_inlines_styles_from_css_string()
    {
        var inliner = new EmailCssInliner("p{color:red;}");
        var html = "<p>Hello</p>";

        var result = inliner.InlineCss(html);
        var normalized = result.Replace(" ", "");

        Assert.Contains("style=\"color:red", normalized);
    }
}
