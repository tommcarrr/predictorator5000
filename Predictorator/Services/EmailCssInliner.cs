using Microsoft.AspNetCore.Hosting;
using PreMailer.Net;

namespace Predictorator.Services;

public class EmailCssInliner
{
    private readonly string? _css;

    public EmailCssInliner(IWebHostEnvironment env)
    {
        var path = Path.Combine(env.WebRootPath, "css", "email.css");
        if (File.Exists(path))
            _css = File.ReadAllText(path);
    }

    public string InlineCss(string html)
    {
        if (string.IsNullOrEmpty(_css))
            return html;
        return PreMailer.Net.PreMailer.MoveCssInline(html, removeStyleElements: true, css: _css).Html;
    }
}
