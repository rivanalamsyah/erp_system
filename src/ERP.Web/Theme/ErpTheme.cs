using MudBlazor;

namespace ERP.Web.Theme;

public static class ErpTheme
{
    public static MudTheme EnterpriseTheme => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1E3A8A",          // Deep Corporate Blue
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#0F766E",        // Professional Teal
            SecondaryContrastText = "#FFFFFF",
            Tertiary = "#475569",         // Slate Gray
            AppbarBackground = "#0F172A",  // Obsidian Slate Header
            AppbarText = "#FFFFFF",
            Background = "#F8FAFC",        // Crisp Light Slate Background
            BackgroundGray = "#F1F5F9",
            Surface = "#FFFFFF",           // Clean Card Surface
            DrawerBackground = "#0B1120",  // Dark Deep Obsidian Sidebar
            DrawerText = "#E2E8F0",
            DrawerIcon = "#94A3B8",
            Success = "#16A34A",           // Emerald Green
            Warning = "#D97706",           // Amber
            Error = "#DC2626",             // Crimson Red
            Info = "#2563EB",              // Azure Blue
            TextPrimary = "#0F172A",
            TextSecondary = "#475569",
            Divider = "#E2E8F0",
            TableLines = "#F1F5F9"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",
            AppbarHeight = "64px"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "Segoe UI", "sans-serif"],
                FontSize = "0.875rem",
                LineHeight = "1.5"
            },
            H5 = new H5Typography
            {
                FontSize = "1.25rem",
                FontWeight = "600",
                LineHeight = "1.3"
            },
            H6 = new H6Typography
            {
                FontSize = "1.05rem",
                FontWeight = "600",
                LineHeight = "1.3"
            },
            Button = new ButtonTypography
            {
                FontWeight = "600",
                TextTransform = "none"
            }
        }
    };
}

