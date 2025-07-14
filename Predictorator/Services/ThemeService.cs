namespace Predictorator.Services;

public class ThemeService
{
    private readonly BrowserInteropService _browser;

    public ThemeService(BrowserInteropService browser)
    {
        _browser = browser;
    }

    public bool IsDarkMode { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var value = await _browser.GetLocalStorageAsync("darkMode");
        if (bool.TryParse(value, out var result))
        {
            IsDarkMode = result;
        }
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
}
