using MudBlazor;

namespace Predictorator.Services;

public class ThemeService
{
    private readonly IBrowserStorage _storage;

    public ThemeService(IBrowserStorage storage)
    {
        _storage = storage;
    }

    public bool IsDarkMode { get; set; }
    public bool IsCeefax { get; set; }

    public MudTheme? CeefaxTheme { get; } = new MudTheme()
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
        var stored = await _storage.GetAsync("darkMode");
        if (stored.HasValue)
        {
            IsDarkMode = stored.Value;
        }

        stored = await _storage.GetAsync("ceefaxMode");
        if (stored.HasValue)
        {
            IsCeefax = stored.Value;
        }

        OnChange?.Invoke();
    }

    public Task ToggleDarkModeAsync() => SetDarkModeAsync(!IsDarkMode);

    public async Task SetDarkModeAsync(bool value)
    {
        if (IsDarkMode != value)
        {
            IsDarkMode = value;
            await _storage.SetAsync("darkMode", value);
            OnChange?.Invoke();
        }
    }

    public Task ToggleCeefaxAsync() => SetCeefaxAsync(!IsCeefax);

    public async Task SetCeefaxAsync(bool value)
    {
        if (IsCeefax != value)
        {
            IsCeefax = value;
            await _storage.SetAsync("ceefaxMode", value);
            OnChange?.Invoke();
        }
    }
}
