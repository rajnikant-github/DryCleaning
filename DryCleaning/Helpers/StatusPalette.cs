using DryCleaning.Models;

namespace DryCleaning.Helpers;

/// <summary>Status chip colours – matches the Figma StatusBadge palette.</summary>
public static class StatusPalette
{
    public static (Color Background, Color Text) For(OrderStatus status) => status switch
    {
        OrderStatus.Created => (C("#F3F4F6"), C("#4B5563")),
        OrderStatus.Confirmed => (C("#FFE3DD"), C("#D13F2A")),
        OrderStatus.PickupAssigned or OrderStatus.PickedUp => (C("#F3E8FF"), C("#6B21A8")),
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
