namespace DryCleaning.Models;

// Read models returned by the provider services (shape of the future API responses).

public enum ProviderOrderFilter
{
    New,          // Created – waiting for Accept / Reject
    Incoming,     // Confirmed, PickupAssigned, PickedUp – driver bringing items
    InProcess,    // ReceivedByProvider, Processing, QualityCheck
    Ready,        // ReadyForDelivery, DeliveryAssigned, OutForDelivery
    Completed,    // Delivered, Completed
    Cancelled,
    Disputed
}

public enum ItemCondition
{
    Good,
    Stained,
    Damaged
}

public sealed class OrderSummary
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public int ItemCount { get; init; }
    public string ItemsText { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? DriverName { get; init; }
    public bool HasOpenDispute { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public DateTime? PickupDate { get; init; }
    public string? PickupSlot { get; init; }
    public bool IsPaid { get; init; }
}

public sealed class OrderItemDetail
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid ServiceItemId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ServiceItemName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public string Icon { get; init; } = "🧺";
    public string? Notes { get; init; }
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public int EstimatedDays { get; init; }

    public string Title => $"{ProductName} × {Quantity}";
    public string Subtitle => $"{CategoryName} · {ServiceItemName}";
}

public sealed class DriverAssignment
{
    public Guid DriverJobId { get; init; }
    public DriverJobType JobType { get; init; }
    public DriverJobStatus Status { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public string DriverPhone { get; init; } = string.Empty;
    public string LicenseNumber { get; init; } = string.Empty;
    public DateTime? AssignedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public sealed class OrderDetail
{
    public required Order Order { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public required Address PickupAddress { get; init; }
    public IReadOnlyList<OrderItemDetail> Items { get; init; } = [];
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

    public bool IsPaid => Payment?.Status == PaymentStatus.Success;
    public int ExpectedItemCount => Items.Sum(i => i.Quantity);
}

public sealed class ActivityEntry
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime At { get; init; }
}

public sealed class ProviderDashboard
{
    public string BusinessName { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public int NewCount { get; init; }
    public int InProcessCount { get; init; }
    public int ReadyCount { get; init; }
    public int CompletedCount { get; init; }
    public int UnreadNotifications { get; init; }
    public OrderSummary? IncomingDriverOrder { get; init; }
    public IReadOnlyList<OrderSummary> TodaysOrders { get; init; } = [];
    public IReadOnlyList<ActivityEntry> RecentActivity { get; init; } = [];
}

public sealed class ReceiveOrderRequest
{
    public int ActualItemCount { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<string> PhotoPaths { get; init; } = [];
}

public sealed class InspectionResult
{
    public Guid OrderItemId { get; init; }
    public string ItemLabel { get; init; } = string.Empty;
    public int ExpectedQuantity { get; init; }
    public int ReceivedQuantity { get; init; }
    public ItemCondition Condition { get; init; }
    public string? Note { get; init; }
    public IReadOnlyList<string> PhotoPaths { get; init; } = [];
}

public sealed class ProductSummary
{
    public required Product Product { get; init; }
    public int ServiceItemCount { get; init; }
    public decimal? StartingPrice { get; init; }
}

public sealed class ProviderProfile
{
    public required Provider Provider { get; init; }
    public required User Owner { get; init; }
    public required Address Address { get; init; }
    public IReadOnlyList<Category> Categories { get; init; } = [];
}
