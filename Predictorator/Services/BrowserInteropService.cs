using Microsoft.JSInterop;

namespace Predictorator.Services;

public class BrowserInteropService
{
    private readonly IJSRuntime _js;

    public BrowserInteropService(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask<bool> GetDarkModeAsync() => _js.InvokeAsync<bool>("app.getDarkMode");

    public ValueTask SaveDarkModeAsync(bool enable) => _js.InvokeVoidAsync("app.saveDarkMode", enable);

    public ValueTask CopyToClipboardTextAsync(string text) => _js.InvokeVoidAsync("app.copyToClipboardText", text);

    public ValueTask CopyToClipboardHtmlAsync(string html) => _js.InvokeVoidAsync("app.copyToClipboardHtml", html);

    public ValueTask<bool> IsMobileDeviceAsync() => _js.InvokeAsync<bool>("app.isMobileDevice");

    public ValueTask AlertAsync(string message) => _js.InvokeVoidAsync("alert", message);
}
