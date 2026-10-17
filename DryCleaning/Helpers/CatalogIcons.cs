using DryCleaning.Models;

namespace DryCleaning.Helpers;

public static class CatalogIcons
{
    public static string ForProduct(string name) => name.ToLowerInvariant() switch
    {
        var n when n.Contains("shirt") => AppIcons.Shirt,
        var n when n.Contains("suit") => AppIcons.UserTie,
        var n when n.Contains("dress") => AppIcons.PersonDress,
        var n when n.Contains("trouser") || n.Contains("pant") => AppIcons.Shirt,
        var n when n.Contains("jacket") || n.Contains("coat") => AppIcons.Shirt,
        var n when n.Contains("sneaker") => AppIcons.ShoePrints,
        var n when n.Contains("boot") => AppIcons.ShoePrints,
        var n when n.Contains("shoe") => AppIcons.ShoePrints,
        var n when n.Contains("curtain") || n.Contains("duvet") => AppIcons.Bed,
        _ => AppIcons.BasketShopping
    };

    public static string ForCategory(string name) =>
        name.Contains("shoe", StringComparison.OrdinalIgnoreCase) ? AppIcons.ShoePrints : AppIcons.Shirt;

    public static string ForNotification(NotificationType type) => type switch
    {
        NotificationType.Order => AppIcons.Box,
        NotificationType.Payment => AppIcons.CreditCard,
        NotificationType.Promo => AppIcons.Gift,
        _ => AppIcons.Bell
    };
}
