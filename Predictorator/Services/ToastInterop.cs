using Microsoft.JSInterop;
using MudBlazor;

namespace Predictorator.Services;

public sealed class ToastInterop : IAsyncDisposable
{
    private readonly ISnackbar _snackbar;
    private readonly IJSRuntime _js;
    private DotNetObjectReference<ToastInterop>? _objRef;

    public ToastInterop(ISnackbar snackbar, IJSRuntime js)
    {
        _snackbar = snackbar;
        _js = js;
    }

    public async Task InitializeAsync()
    {
        _objRef ??= DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("app.registerToastHandler", _objRef);
    }

    [JSInvokable]
    public Task ShowToast(string message)
    {
        _snackbar.Add(message, Severity.Normal);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _objRef?.Dispose();
        return ValueTask.CompletedTask;
    }
}
