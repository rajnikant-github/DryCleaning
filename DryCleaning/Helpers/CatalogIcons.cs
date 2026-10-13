using DryCleaning.Models;

namespace DryCleaning.Helpers;

public static class CatalogIcons
{
    public static string ForProduct(string name) => name.ToLowerInvariant() switch
    {
        var n when n.Contains("shirt") => "👔",
        var n when n.Contains("suit") => "🤵",
        var n when n.Contains("dress") => "👗",
        var n when n.Contains("trouser") || n.Contains("pant") => "👖",
        var n when n.Contains("jacket") || n.Contains("coat") => "🧥",
        var n when n.Contains("sneaker") => "👟",
        var n when n.Contains("boot") => "🥾",
        var n when n.Contains("shoe") => "👞",
        var n when n.Contains("curtain") || n.Contains("duvet") => "🛏️",
        _ => "🧺"
    };

    public static string ForCategory(string name) =>
        name.Contains("shoe", StringComparison.OrdinalIgnoreCase) ? "👟" : "👔";

    public static string ForNotification(NotificationType type) => type switch
    {
        NotificationType.Order => "📦",
        NotificationType.Payment => "💳",
        NotificationType.Promo => "🎁",
        _ => "🔔"
    };
}
