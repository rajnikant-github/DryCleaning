using DryCleaning.Models;

namespace DryCleaning.Helpers;

public static class TimeFormat
{
    public static string Relative(DateTime at)
    {
        var span = DateTime.Now - at;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24 && at.Date == DateTime.Today) return $"{(int)span.TotalHours} h ago";
        if (at.Date == DateTime.Today.AddDays(-1)) return $"Yesterday, {at:HH:mm}";
        return at.ToString("d MMM, HH:mm");
    }

    public static string Greeting(DateTime now) => now.Hour switch
    {
        < 12 => "Good morning",
        < 17 => "Good afternoon",
        _ => "Good evening"
    };
}

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

public static class StringHelpers
{
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }
}

/// <summary>Status chip colours – matches the Figma StatusBadge palette.</summary>
public static class StatusPalette
{
    public static (Color Background, Color Text) For(OrderStatus status) => status switch
    {
        OrderStatus.Created => (C("#F3F4F6"), C("#4B5563")),
        OrderStatus.Confirmed => (C("#DBEAFE"), C("#1D4ED8")),
        OrderStatus.PickupAssigned or OrderStatus.PickedUp => (C("#E0F2FE"), C("#0369A1")),
        OrderStatus.ReceivedByProvider => (C("#EDE9FE"), C("#6D28D9")),
        OrderStatus.Processing => (C("#FEF3C7"), C("#B45309")),
        OrderStatus.QualityCheck => (C("#F3E8FF"), C("#7E22CE")),
        OrderStatus.ReadyForDelivery => (C("#D1FAE5"), C("#047857")),
        OrderStatus.DeliveryAssigned => (C("#CCFBF1"), C("#0F766E")),
        OrderStatus.OutForDelivery => (C("#CFFAFE"), C("#0E7490")),
        OrderStatus.Delivered or OrderStatus.Completed => (C("#DCFCE7"), C("#15803D")),
        OrderStatus.Cancelled => (C("#FEE2E2"), C("#DC2626")),
        OrderStatus.Disputed => (C("#FFEDD5"), C("#C2410C")),
        _ => (C("#F3F4F6"), C("#4B5563"))
    };

    private static Color C(string hex) => Color.FromArgb(hex);
}
