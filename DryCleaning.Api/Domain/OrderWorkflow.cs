using static DryCleaning.Api.Domain.OrderStatus;

namespace DryCleaning.Api.Domain;

/// <summary>Allowed order status transitions (same rules as the mobile app).</summary>
public static class OrderWorkflow
{
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
        [Completed] = [Disputed],
        [Cancelled] = [],
        [Disputed] = [],
    };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static void EnsureCanTransition(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
            throw new DomainException($"Order status cannot change from '{from}' to '{to}'.");
    }

    /// <summary>Statuses an admin can force when resolving a dispute.</summary>
    public static bool IsTerminal(OrderStatus status) => status is Completed or Cancelled;
}
