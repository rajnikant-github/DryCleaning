namespace DryCleaning.Models;

public sealed class OrderDetail
{
    public required Order Order { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public required Address PickupAddress { get; init; }
    public IReadOnlyList<OrderItemDetail> Items { get; init; } = [];

    /// <summary>Extra charges the admin had switched on when the order was placed (already in the order total).</summary>
    public IReadOnlyList<ExtraChargeLine> ExtraCharges { get; init; } = [];
    public IReadOnlyList<OrderStatusHistory> History { get; init; } = [];
    public IReadOnlyList<OrderPhoto> Photos { get; init; } = [];
    public DriverAssignment? PickupDriver { get; init; }
    public DriverAssignment? DeliveryDriver { get; init; }
    public Payment? Payment { get; init; }
    public Dispute? OpenDispute { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string ProviderPhone { get; init; } = string.Empty;
    public Address? ProviderAddress { get; init; }

    /// <summary>Customer-only: code shown to the pickup driver (QR verification).</summary>
    public string? PickupCode { get; init; }

    /// <summary>Customer-only: OTP given to the delivery driver.</summary>
    public string? DeliveryOtp { get; init; }

    /// <summary>Set while a pickup/delivery job is waiting on the driver marketplace.</summary>
    public DriverJobType? AwaitingDriverFor { get; init; }

    public bool IsPaid => Payment?.Status == PaymentStatus.Success;
    public int ExpectedItemCount => Items.Sum(i => i.Quantity);
}
