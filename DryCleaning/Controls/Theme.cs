using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using ViewState = DryCleaning.ViewModels.ViewState;

namespace DryCleaning.Controls;

internal static class Theme
{
    public static Color Resource(string key, string fallback)
    {
        var resources = Application.Current?.Resources;
        if (resources is not null && resources.TryGetValue(key, out var value) && value is Color color)
            return color;

        return Color.FromArgb(fallback);
    }

    public static Color Primary => Resource("Primary", "#FF725E");
    public static Color Border => Resource("BorderColor", "#E5E2D8");
    public static Color Muted => Resource("Muted", "#F0EDE6");
    public static Color MutedForeground => Resource("MutedForeground", "#6B7280");
    public static Color Foreground => Resource("AppForeground", "#111827");
    public static Color Card => Resource("CardColor", "#FFFFFF");
    public static Color Accent => Resource("Accent", "#0EA5E9");
}
