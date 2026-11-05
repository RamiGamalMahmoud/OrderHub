using MudBlazor;

namespace OrderHub.Web.Services;

/// <summary>
/// Single source of truth for the programmatic MudTheme (redesign task T-A3).
/// Both <c>MainLayout.razor</c> and <c>StandaloneLayout.razor</c> reference this —
/// the authenticated shell and standalone pages (login / setup) can never drift.
/// Finer component styling that exceeds what the theme expresses lives in
/// <c>wwwroot/css/mud-blazor-overrides.css</c> (task T-A2).
/// </summary>
public static class OrderHubTheme
{
    /// <summary>
    /// Locked decisions: Tajawal everywhere (local font, no runtime fetching),
    /// primary = Brand Blue 600, background = #f8fafc, default radius = 12.
    /// </summary>
    public static MudTheme Current => new()
    {
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Tajawal", "sans-serif"]
            }
        },
        PaletteLight = new PaletteLight()
        {
            Primary = "#2563EB",        // Brand blue-600 (matches @theme --color-brand-600; hover states derive from palette)
            Secondary = "#64748B",      // Slate-500 secondary text
            Background = "#F8FAFC",     // Slate-50 page background
            Surface = "#FFFFFF",        // Cards, menus and appbar
            AppbarBackground = "#FFFFFF",
            AppbarText = "#0F172A",
            DrawerBackground = "#FFFFFF"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px" // cards family radius; inputs stay 8px via mud-blazor-overrides.css
        }
    };
}
