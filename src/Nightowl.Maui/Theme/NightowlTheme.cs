using MudBlazor;

namespace Nightowl.Maui.Theme;

public static class NightowlTheme
{
    public static MudTheme CreateTheme()
    {
        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#6366f1",
                Secondary = "#8b5cf6",
                Tertiary = "#06b6d4",
                AppbarBackground = "#ffffff",
                AppbarText = "#1e293b",
                Background = "#f8fafc",
                Surface = "#ffffff",
                DrawerBackground = "#ffffff",
                DrawerText = "#334155",
                Success = "#10b981",
                Info = "#3b82f6",
                Warning = "#f59e0b",
                Error = "#ef4444",
                TextPrimary = "#0f172a",
                TextSecondary = "#64748b"
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#818cf8",
                Secondary = "#a78bfa",
                Tertiary = "#22d3ee",
                AppbarBackground = "#090d16",
                AppbarText = "#f1f5f9",
                Background = "#0b0f19",
                Surface = "#111827",
                DrawerBackground = "#0d131f",
                DrawerText = "#cbd5e1",
                Success = "#34d399",
                Info = "#60a5fa",
                Warning = "#fbbf24",
                Error = "#f87171",
                TextPrimary = "#f8fafc",
                TextSecondary = "#94a3b8"
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "12px",
                AppbarHeight = "64px"
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[] { "Plus Jakarta Sans", "Roboto", "sans-serif" }
                },
                H1 = new H1Typography
                {
                    FontFamily = new[] { "Cinzel", "serif" },
                    FontWeight = "700"
                },
                H2 = new H2Typography
                {
                    FontFamily = new[] { "Cinzel", "serif" },
                    FontWeight = "700"
                },
                H3 = new H3Typography
                {
                    FontFamily = new[] { "Cinzel", "serif" },
                    FontWeight = "600"
                },
                H4 = new H4Typography
                {
                    FontFamily = new[] { "Plus Jakarta Sans", "sans-serif" },
                    FontWeight = "700"
                },
                H5 = new H5Typography
                {
                    FontFamily = new[] { "Plus Jakarta Sans", "sans-serif" },
                    FontWeight = "600"
                },
                H6 = new H6Typography
                {
                    FontFamily = new[] { "Plus Jakarta Sans", "sans-serif" },
                    FontWeight = "600"
                }
            }
        };
    }
}
