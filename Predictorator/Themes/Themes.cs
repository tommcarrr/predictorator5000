using MudBlazor;

namespace Predictorator;

public static class Themes
{
    public static MudTheme FootballPredictorTheme = new MudTheme()
    {
        PaletteLight = new PaletteLight()
        {
            // Primary brand color – a rich, dark green
            Primary = "#1E5F3E",
            PrimaryContrastText = "#FFFFFF",

            // Secondary/Accent color – a vibrant amber
            Secondary = "#FFB300",
            SecondaryContrastText = "#212121",

            // Darker shades for hover, active states
            PrimaryDarken = "#164731",
            SecondaryDarken = "#CC9500",

            // Backgrounds
            Background = "#F4F6F8",      // very light grey
            Surface = "#FFFFFF",         // card and panel surfaces
            AppbarBackground = "#1E5F3E",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#212121",

            // Text
            TextPrimary = "#212121",
            TextSecondary = "#555555",
            TextDisabled = "#9E9E9E",

            // Success / Info / Warning / Error
            Success = "#4CAF50",
            Info = "#2196F3",
            Warning = "#FB8C00",
            Error = "#E53935",

            // Lines & dividers
            Divider = "#E0E0E0",
            TableLines = "#BDBDBD",
            // Transparency
            OverlayDark = "rgba(0, 0, 0, 0.6)"
        },
        PaletteDark = new PaletteDark()
        {
            Primary = "#1E5F3E",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#FFB300",
            SecondaryContrastText = "#212121",
            Background = "#121212",
            Surface = "#1E1E1E",
            AppbarBackground = "#1E5F3E",
            DrawerBackground = "#1E1E1E",
            DrawerText = "#FFFFFF",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#BBBBBB",
            TextDisabled = "#777777",
            Success = "#4CAF50",
            Info = "#2196F3",
            Warning = "#FB8C00",
            Error = "#E53935",
            Divider = "#424242",
            TableLines = "#616161",
            OverlayDark = "rgba(0, 0, 0, 0.6)"
        },
        Typography = new Typography()
        {
            Default = new Default()
            {
                FontFamily = new[] { "Segoe UI", "Roboto", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = 400,
                LineHeight = 1.43
            },
            H1 = new H1()
            {
                FontFamily = new[] { "Montserrat", "sans-serif" },
                FontSize = "2.125rem",
                FontWeight = 700,
                LineHeight = 1.235
            },
            H2 = new H2()
            {
                FontFamily = new[] { "Montserrat", "sans-serif" },
                FontSize = "1.75rem",
                FontWeight = 700,
                LineHeight = 1.334
            },
            Button = new Button()
            {
                FontFamily = new[] { "Segoe UI", "sans-serif" },
                FontSize = "0.875rem",
                FontWeight = 600,
                TextTransform = "uppercase"
            }
        },
        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "4px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "260px"
        },
        Shadows = new Shadow()
        {
            Elevation = new[]
            {
                "none",
                "0px 1px 3px rgba(0,0,0,0.12), 0px 1px 2px rgba(0,0,0,0.24)",
                "0px 3px 6px rgba(0,0,0,0.16), 0px 3px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)",
                "0px 10px 20px rgba(0,0,0,0.19), 0px 6px 6px rgba(0,0,0,0.23)"
            }
        },
        ZIndex = new ZIndex()
        {
            AppBar = 1100,
            Drawer = 1000,
            Dialog = 1300,
            Snackbar = 1400,
            Tooltip = 1500
        }
    };
}
