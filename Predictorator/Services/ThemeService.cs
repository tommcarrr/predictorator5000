namespace Predictorator.Services;

public class ThemeService
{
    private readonly BrowserInteropService _browser;

    public bool IsDarkMode { get; private set; }

    public event Action? OnChange;

    public ThemeService(BrowserInteropService browser)
    {
        _browser = browser;
    }

    public async Task InitializeAsync()
    {
        IsDarkMode = await _browser.GetDarkModeAsync();
        OnChange?.Invoke();
    }

    public async Task ToggleDarkModeAsync()
    {
        IsDarkMode = !IsDarkMode;
        await _browser.SaveDarkModeAsync(IsDarkMode);
        OnChange?.Invoke();
    }

    public async Task SetDarkModeAsync(bool value)
    {
        if (IsDarkMode != value)
        {
            IsDarkMode = value;
            await _browser.SaveDarkModeAsync(value);
            OnChange?.Invoke();
        }
    }
}
