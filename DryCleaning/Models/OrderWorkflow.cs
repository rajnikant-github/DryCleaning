using static DryCleaning.Models.OrderStatus;

namespace DryCleaning.Models;

/// <summary>
/// Single source of truth for allowed order status transitions and status grouping.
/// </summary>
public static class OrderWorkflow
{
    /// <summary>The happy-path lifecycle used for timelines.</summary>
    public static readonly IReadOnlyList<OrderStatus> Lifecycle =
    [
        Created, Confirmed, PickupAssigned, PickedUp, ReceivedByProvider, Processing,
        QualityCheck, ReadyForDelivery, DeliveryAssigned, OutForDelivery, Delivered, Completed
    ];

    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [Created] = [Confirmed, Cancelled],
        [Confirmed] = [PickupAssigned, Cancelled, Disputed],
        [PickupAssigned] = [Confirmed, PickedUp, Cancelled, Disputed],
        [PickedUp] = [ReceivedByProvider, Disputed],
        [ReceivedByProvider] = [Processing, Disputed],
        [Processing] = [QualityCheck, Disputed],
        [QualityCheck] = [ReadyForDelivery, Processing, Disputed],
        [ReadyForDelivery] = [DeliveryAssigned, Disputed],
        [DeliveryAssigned] = [ReadyForDelivery, OutForDelivery, Disputed],
        [OutForDelivery] = [Delivered, Disputed],
        [Delivered] = [Completed, Disputed],
        [Completed] = [],
        [Cancelled] = [],
        [Disputed] = [],
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static void EnsureCanTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException(
                $"This order is '{from.DisplayName()}' and can't be moved to '{to.DisplayName()}'.");
    }

    public static bool Matches(this ProviderOrderFilter filter, OrderStatus status) => filter switch
    {
        ProviderOrderFilter.New => status == Created,
        ProviderOrderFilter.Incoming => status is Confirmed or PickupAssigned or PickedUp,
        ProviderOrderFilter.InProcess => status is ReceivedByProvider or Processing or QualityCheck,
        ProviderOrderFilter.Ready => status is ReadyForDelivery or DeliveryAssigned or OutForDelivery,
        ProviderOrderFilter.Completed => status is Delivered or Completed,
        ProviderOrderFilter.Cancelled => status == Cancelled,
        ProviderOrderFilter.Disputed => status == Disputed,
        _ => false
    };

    public static string DisplayName(this OrderStatus status) => status switch
    {
        Created => "New",
        Confirmed => "Confirmed",
        PickupAssigned => "Pickup Assigned",
        PickedUp => "Picked Up",
        ReceivedByProvider => "Received",
        Processing => "Processing",
        QualityCheck => "Quality Check",
        ReadyForDelivery => "Ready for Delivery",
        DeliveryAssigned => "Delivery Assigned",
        OutForDelivery => "Out for Delivery",
        Delivered => "Delivered",
        Completed => "Completed",
        Cancelled => "Cancelled",
        Disputed => "Disputed",
        _ => status.ToString()
    };

    /// <summary>Wording used on the timeline.</summary>
    public static string TimelineTitle(this OrderStatus status) => status switch
    {
        Created => "Order placed",
        Confirmed => "Accepted by you",
        PickupAssigned => "Pickup driver assigned",
        PickedUp => "Picked up from customer",
        ReceivedByProvider => "Received at your store",
        Processing => "Processing",
        QualityCheck => "Quality check",
        ReadyForDelivery => "Ready for delivery",
        DeliveryAssigned => "Delivery driver assigned",
        OutForDelivery => "Out for delivery",
        Delivered => "Delivered to customer",
        Completed => "Completed",
        Cancelled => "Cancelled",
        Disputed => "Disputed",
        _ => status.ToString()
    };

    /// <summary>Customer-facing timeline wording.</summary>
    public static string CustomerTimelineTitle(this OrderStatus status) => status switch
    {
        Created => "Order placed",
        Confirmed => "Confirmed by provider",
        PickupAssigned => "Driver assigned",
        PickedUp => "Picked up from you",
        ReceivedByProvider => "Received by provider",
        Processing => "Cleaning in progress",
        QualityCheck => "Quality check",
        ReadyForDelivery => "Ready for delivery",
        DeliveryAssigned => "Delivery driver assigned",
        OutForDelivery => "Out for delivery",
        Delivered => "Delivered",
        Completed => "Completed",
        Cancelled => "Cancelled",
        Disputed => "On hold – dispute raised",
        _ => status.ToString()
    };

    /// <summary>One-line explanation of where a customer's order is.</summary>
    public static string CustomerStatusMessage(this OrderStatus status, bool isPaid) => status switch
    {
        Created when !isPaid => "Waiting for payment to send your order to the provider.",
        Created => "Waiting for the provider to accept your order.",
        Confirmed => "Accepted! We're finding a driver to collect your items.",
        PickupAssigned => "A driver is on the way to collect your items.",
        PickedUp => "Your items are on the way to the provider.",
        ReceivedByProvider => "The provider has received your items.",
        Processing => "Your items are being cleaned.",
        QualityCheck => "Final quality check before packing.",
        ReadyForDelivery => "Cleaned and packed – a driver will be assigned soon.",
        DeliveryAssigned => "A driver is collecting your order from the provider.",
        OutForDelivery => "Your order is on its way. Share your delivery OTP with the driver.",
        Delivered or Completed => "Delivered. Thank you for using E Dry Cleaners!",
        Cancelled => "This order was cancelled.",
        Disputed => "On hold while E Dry Cleaners support reviews an issue.",
        _ => string.Empty
    };
}
