using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;

namespace Predictorator.Services;

public static class ToastInterop
{
    private static IServiceProvider? _provider;

    public static void Configure(IServiceProvider provider)
    {
        _provider = provider;
    }

    [JSInvokable]
    public static Task ShowToast(string message)
    {
        if (_provider == null)
            return Task.CompletedTask;

        using var scope = _provider.CreateScope();
        var snackbar = scope.ServiceProvider.GetService<ISnackbar>();
        snackbar?.Add(message, Severity.Normal);
        return Task.CompletedTask;
    }
}
