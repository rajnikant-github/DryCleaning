using DryCleaning.Models;

namespace DryCleaning.Helpers;

public static class CatalogIcons
{
    public static string ForProduct(string name) => name.ToLowerInvariant() switch
    {
        var n when n.Contains("shirt") => FontAwesome.Shirt,
        var n when n.Contains("suit") => FontAwesome.UserTie,
        var n when n.Contains("dress") => FontAwesome.PersonDress,
        var n when n.Contains("trouser") || n.Contains("pant") => FontAwesome.Shirt,
        var n when n.Contains("jacket") || n.Contains("coat") => FontAwesome.Shirt,
        var n when n.Contains("sneaker") => FontAwesome.ShoePrints,
        var n when n.Contains("boot") => FontAwesome.ShoePrints,
        var n when n.Contains("shoe") => FontAwesome.ShoePrints,
        var n when n.Contains("curtain") || n.Contains("duvet") => FontAwesome.Bed,
        _ => FontAwesome.BasketShopping
    };

    public static string ForCategory(string name) =>
        name.Contains("shoe", StringComparison.OrdinalIgnoreCase) ? FontAwesome.ShoePrints : FontAwesome.Shirt;

    public static string ForNotification(NotificationType type) => type switch
    {
        NotificationType.Order => FontAwesome.Box,
        NotificationType.Payment => FontAwesome.CreditCard,
        NotificationType.Promo => FontAwesome.Gift,
        _ => FontAwesome.Bell
    };
}
