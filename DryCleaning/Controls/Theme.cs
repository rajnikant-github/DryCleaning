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

    public static Color Primary => Resource("Primary", "#FF5E5E");
    public static Color PrimaryDark => Resource("PrimaryDark", "#C93643");
    public static Color PrimarySoft => Resource("PrimarySoft", "#FFF0EF");
    public static Color Border => Resource("BorderColor", "#E9EBF1");
    public static Color Muted => Resource("Muted", "#EFF0F5");
    public static Color MutedForeground => Resource("MutedForeground", "#707887");
    public static Color Foreground => Resource("AppForeground", "#202B3D");
    public static Color Card => Resource("CardColor", "#FFFFFF");
    public static Color Accent => Resource("Accent", "#C93643");
}
