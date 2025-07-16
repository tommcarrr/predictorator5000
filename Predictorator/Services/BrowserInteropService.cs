using Microsoft.JSInterop;

namespace Predictorator.Services;

public class BrowserInteropService
{
    private readonly IJSRuntime _js;

    public BrowserInteropService(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask<bool> CopyToClipboardTextAsync(string text) =>
        _js.InvokeAsync<bool>("app.copyToClipboardText", text);

    public ValueTask<bool> CopyToClipboardHtmlAsync(string html) =>
        _js.InvokeAsync<bool>("app.copyToClipboardHtml", html);

    public ValueTask<bool> IsMobileDeviceAsync() => _js.InvokeAsync<bool>("app.isMobileDevice");

    public ValueTask AlertAsync(string message) => _js.InvokeVoidAsync("alert", message);

}
