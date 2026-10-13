namespace DryCleaning.Models;

// Read models for the Driver app (shape of the /api/driver responses).

public enum DriverJobFilter
{
    Available,   // Pending jobs on the marketplace
    Mine,        // Assigned / InProgress
    Completed
}

public enum EarningsPeriod
{
    Week,
    Month
}

/// <summary>The next thing the driver has to do on a job (derived from job type + status).</summary>
public enum DriverJobStep
{
    Accept,               // Pending on the marketplace
    CollectFromCustomer,  // Pickup · Assigned      → go to customer, verify items, pickup code
    DropAtProvider,       // Pickup · InProgress    → go to provider, hand over
    CollectFromProvider,  // Delivery · Assigned    → go to provider, collect package
    DeliverToCustomer,    // Delivery · InProgress  → go to customer, delivery OTP
    Done,                 // Completed
    Unavailable           // Cancelled, taken by someone else or order on hold
}

public sealed class DriverJobSummary
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DriverJobType JobType { get; init; }
    public DriverJobStatus Status { get; init; }
    public OrderStatus OrderStatus { get; init; }

    public string FromName { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromArea { get; init; } = string.Empty;
    public string ToName { get; init; } = string.Empty;
    public string ToAddress { get; init; } = string.Empty;
    public string ToArea { get; init; } = string.Empty;

    public int ItemCount { get; init; }
    public string ItemsText { get; init; } = string.Empty;
    public decimal Earnings { get; init; }
    public double DistanceKm { get; init; }
    public DateTime? PickupDate { get; init; }
    public string? PickupSlot { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? AssignedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsOnHold { get; init; }

    public bool IsPickup => JobType == DriverJobType.Pickup;
    public string TypeIcon => IsPickup ? "📦" : "🚚";
    public string TypeText => IsPickup ? "Pickup" : "Delivery";
}

public sealed class DriverJobDetail
{
    public required DriverJobSummary Job { get; init; }
    public required Address From { get; init; }
    public required Address To { get; init; }
    public string FromPhone { get; init; } = string.Empty;
    public string ToPhone { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public IReadOnlyList<OrderItemDetail> Items { get; init; } = [];
    public IReadOnlyList<OrderStatusHistory> History { get; init; } = [];
    public DriverJobStep NextStep { get; init; }

    /// <summary>True when this job belongs to the signed-in driver.</summary>
    public bool IsMine { get; init; }

    /// <summary>Mock/demo only (Features.ShowDemoActions): the customer's pickup code or delivery OTP, so the flow can be tested on one phone.</summary>
    public string? DemoCustomerCode { get; init; }

    /// <summary>Where the driver is heading for the next step: the job's start (From) or its drop-off (To).</summary>
    public bool HeadingToStart => NextStep is DriverJobStep.Accept or DriverJobStep.CollectFromCustomer or DriverJobStep.CollectFromProvider;

    public Address Destination => HeadingToStart ? From : To;
    public string DestinationName => HeadingToStart ? Job.FromName : Job.ToName;
    public string DestinationPhone => HeadingToStart ? FromPhone : ToPhone;
}

public sealed class DriverDashboard
{
    public string DriverName { get; init; } = string.Empty;
    public bool IsOnline { get; init; }
    public int AvailableCount { get; init; }
    public DriverJobDetail? ActiveJob { get; init; }
    public int CompletedToday { get; init; }
    public decimal EarningsToday { get; init; }
    public int UnreadNotifications { get; init; }
    public IReadOnlyList<DriverJobSummary> TodaysJobs { get; init; } = [];
}

public sealed record EarningsBar(string Label, decimal Amount);

public sealed class DriverEarnings
{
    public EarningsPeriod Period { get; init; }
    public string PeriodText { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public int JobCount { get; init; }
    public int PickupCount { get; init; }
    public int DeliveryCount { get; init; }
    public decimal RatePerJob { get; init; }
    public decimal AveragePerJob => JobCount == 0 ? 0 : Total / JobCount;
    public IReadOnlyList<EarningsBar> Bars { get; init; } = [];
    public IReadOnlyList<DriverJobSummary> Jobs { get; init; } = [];
}

public sealed class DriverProfile
{
    public required User User { get; init; }
    public required Driver Driver { get; init; }
    public Address? BaseAddress { get; init; }
    public int CompletedJobs { get; init; }
    public decimal TotalEarnings { get; init; }
}

/// <summary>What the driver checked at the customer's door (D05).</summary>
public sealed class DriverItemCheck
{
    public Guid OrderItemId { get; init; }
    public string ItemLabel { get; init; } = string.Empty;
    public int ExpectedQuantity { get; init; }
    public int CountedQuantity { get; init; }
    public ItemCondition Condition { get; init; }
    public string? Note { get; init; }
}

public sealed class DriverPickupRequest
{
    public string PickupCode { get; init; } = string.Empty;
    public IReadOnlyList<DriverItemCheck> Items { get; init; } = [];
    public IReadOnlyList<string> PhotoPaths { get; init; } = [];
    public string? Notes { get; init; }
}
