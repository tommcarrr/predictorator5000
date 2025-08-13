using PreMailer.Net;

namespace Predictorator.Core.Services;

public class EmailCssInliner
{
    private readonly string _css;

    private const string DefaultCss = "body { font-family: Arial, sans-serif; }\na { color: #1e88e5; }\n";

    public EmailCssInliner() : this(DefaultCss)
    {
    }

    public EmailCssInliner(string css)
    {
        _css = css;
    }

    public string InlineCss(string html)
    {
        if (string.IsNullOrEmpty(_css))
            return html;
        return PreMailer.Net.PreMailer.MoveCssInline(html, removeStyleElements: true, css: _css).Html;
    }
}
