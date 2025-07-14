namespace Predictorator.Services;

public class ThemeService
{
    public bool IsDarkMode { get; private set; }

    public event Action? OnChange;

    public void ToggleDarkMode()
    {
        SetDarkMode(!IsDarkMode);
    }

    public void SetDarkMode(bool value)
    {
        if (IsDarkMode != value)
        {
            IsDarkMode = value;
            OnChange?.Invoke();
        }
    }
}
