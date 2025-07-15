using MudBlazor;

namespace Predictorator.Services;

public class ThemeService
{
    private readonly BrowserInteropService _browser;

    public ThemeService(BrowserInteropService browser)
    {
        _browser = browser;
    }

    public bool IsDarkMode { get; private set; }
    public bool IsCeefax { get; private set; }

    public MudTheme CeefaxTheme { get; } = new MudTheme()
    {
        PaletteDark = new PaletteDark()
        {
            Background = "#000000",
            Surface = "#000000",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#CCCCCC",
            Primary = "#0000FF",
            Secondary = "#00FF00",
            Info = "#00FFFF",
            Success = "#00FF00",
            Warning = "#FFFF00",
            Error = "#FF0000",
            AppbarBackground = "#0000FF",
            AppbarText = "#00FF00",
            DrawerBackground = "#000000",
            DrawerText = "#FFFFFF",
            ActionDefault = "#FFFFFF",
            ActionDisabled = "#555555",
            ActionDisabledBackground = "#222222"
        }
    };

    public MudTheme? CurrentTheme => IsCeefax ? CeefaxTheme : null;

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var value = await _browser.GetLocalStorageAsync("darkMode");
        if (bool.TryParse(value, out var result))
        {
            IsDarkMode = result;
        }

        value = await _browser.GetLocalStorageAsync("ceefaxMode");
        if (bool.TryParse(value, out result))
        {
            IsCeefax = result;
        }

        OnChange?.Invoke();
    }

    public Task ToggleDarkModeAsync() => SetDarkModeAsync(!IsDarkMode);

    public async Task SetDarkModeAsync(bool value)
    {
        if (IsDarkMode != value)
        {
            IsDarkMode = value;
            await _browser.SetLocalStorageAsync("darkMode", value.ToString().ToLowerInvariant());
            OnChange?.Invoke();
        }
    }

    public Task ToggleCeefaxAsync() => SetCeefaxAsync(!IsCeefax);

    public async Task SetCeefaxAsync(bool value)
    {
        if (IsCeefax != value)
        {
            IsCeefax = value;
            await _browser.SetLocalStorageAsync("ceefaxMode", value.ToString().ToLowerInvariant());
            OnChange?.Invoke();
        }
    }
}
