using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

// Item ViewModels for the Driver screens (each carries its own commands).

public sealed class DriverJobCardItem
{
    public DriverJobCardItem(DriverJobSummary job, Func<DriverJobSummary, Task> open, Func<DriverJobSummary, Task>? accept = null)
    {
        Job = job;
        OpenCommand = new AsyncRelayCommand(() => open(job));
        AcceptCommand = new AsyncRelayCommand(() => accept?.Invoke(job) ?? Task.CompletedTask);
        ShowAccept = accept is not null && job.Status == DriverJobStatus.Pending;
    }

    public DriverJobSummary Job { get; }

    public string TypeIcon => Job.TypeIcon;
    public string TypeText => Job.TypeText;
    public bool IsPickup => Job.IsPickup;
    public string OrderNumber => Job.OrderNumber;
    public string FromName => Job.FromName;
    public string FromArea => Job.FromArea;
    public string ToName => Job.ToName;
    public string ToArea => Job.ToArea;
    public string ItemsText => Job.ItemCount == 1 ? "1 item" : $"{Job.ItemCount} items";
    public decimal Earnings => Job.Earnings;
    public string DistanceText => Geo.DistanceText(Job.DistanceKm);

    public string WhenText => Job.Status switch
    {
        DriverJobStatus.Completed when Job.CompletedAt is { } at => $"Completed {TimeFormat.Relative(at)}",
        DriverJobStatus.Cancelled => "Cancelled",
        _ when Job.IsPickup && Job.PickupDate is { } date =>
            $"{DriverText.Day(date)}{(string.IsNullOrEmpty(Job.PickupSlot) ? "" : $" · {Job.PickupSlot}")}",
        _ => $"Posted {TimeFormat.Relative(Job.CreatedAt)}"
    };

    public string StatusText => DriverText.JobStatus(Job.Status, Job.JobType);
    public bool ShowStatus => Job.Status != DriverJobStatus.Pending;
    public bool IsOnHold => Job.IsOnHold;
    public bool ShowAccept { get; }

    public IAsyncRelayCommand OpenCommand { get; }
    public IAsyncRelayCommand AcceptCommand { get; }
}

/// <summary>D05 – one order line checked at the customer's door.</summary>
public sealed partial class DriverItemCheckItem : ObservableObject
{
    public DriverItemCheckItem(OrderItemDetail item)
    {
        Item = item;
        countedQuantity = item.Quantity;
    }

    public OrderItemDetail Item { get; }
    public string Icon => Item.Icon;
    public string Title => Item.ProductName;
    public string Subtitle => $"{Item.CategoryName} · {Item.ServiceItemName}";
    public int ExpectedQuantity => Item.Quantity;
    public string ExpectedText => $"Expected {Item.Quantity}";
    public string? CustomerNotes => Item.Notes;
    public bool HasCustomerNotes => Item.HasNotes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDiscrepancy))]
    [NotifyPropertyChangedFor(nameof(DiscrepancyText))]
    private int countedQuantity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGood))]
    [NotifyPropertyChangedFor(nameof(IsStained))]
    [NotifyPropertyChangedFor(nameof(IsDamaged))]
    [NotifyPropertyChangedFor(nameof(NeedsNote))]
    private ItemCondition condition = ItemCondition.Good;

    [ObservableProperty]
    private string note = string.Empty;

    public bool IsGood => Condition == ItemCondition.Good;
    public bool IsStained => Condition == ItemCondition.Stained;
    public bool IsDamaged => Condition == ItemCondition.Damaged;
    public bool NeedsNote => Condition != ItemCondition.Good || HasDiscrepancy;
    public bool HasDiscrepancy => CountedQuantity != ExpectedQuantity;
    public string DiscrepancyText => CountedQuantity < ExpectedQuantity
        ? $"{ExpectedQuantity - CountedQuantity} missing"
        : $"{CountedQuantity - ExpectedQuantity} extra";

    partial void OnCountedQuantityChanged(int value) => OnPropertyChanged(nameof(NeedsNote));

    [RelayCommand]
    private void SetCondition(string value)
    {
        if (Enum.TryParse<ItemCondition>(value, out var parsed))
            Condition = parsed;
    }

    public DriverItemCheck ToCheck() => new()
    {
        OrderItemId = Item.Id,
        ItemLabel = $"{Item.ProductName} ({Item.ServiceItemName})",
        ExpectedQuantity = ExpectedQuantity,
        CountedQuantity = CountedQuantity,
        Condition = Condition,
        Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
    };
}

/// <summary>One bar of the earnings chart (D13).</summary>
public sealed class EarningsBarItem(EarningsBar bar, decimal max)
{
    private const double MaxBarHeight = 120;

    public string Label => bar.Label;
    public decimal Amount => bar.Amount;
    public double BarHeight => max <= 0 ? 4 : Math.Max(4, (double)(bar.Amount / max) * MaxBarHeight);
    public bool HasAmount => bar.Amount > 0;
    public bool IsEmpty => bar.Amount <= 0;
}

public static class DriverText
{
    public static string Day(DateTime date) =>
        date.Date == DateTime.Today ? "Today"
        : date.Date == DateTime.Today.AddDays(1) ? "Tomorrow"
        : date.Date == DateTime.Today.AddDays(-1) ? "Yesterday"
        : date.ToString("ddd d MMM");

    public static string JobStatus(DriverJobStatus status, DriverJobType type) => (status, type) switch
    {
        (DriverJobStatus.Pending, _) => "Available",
        (DriverJobStatus.Assigned, DriverJobType.Pickup) => "Go to customer",
        (DriverJobStatus.Assigned, DriverJobType.Delivery) => "Go to provider",
        (DriverJobStatus.InProgress, DriverJobType.Pickup) => "Items on board",
        (DriverJobStatus.InProgress, DriverJobType.Delivery) => "Out for delivery",
        (DriverJobStatus.Completed, _) => "Completed",
        _ => "Cancelled"
    };

    /// <summary>Short instruction for the next step.</summary>
    public static string StepTitle(DriverJobStep step) => step switch
    {
        DriverJobStep.Accept => "Accept this job",
        DriverJobStep.CollectFromCustomer => "Collect items from the customer",
        DriverJobStep.DropAtProvider => "Drop items at the provider",
        DriverJobStep.CollectFromProvider => "Collect the order from the provider",
        DriverJobStep.DeliverToCustomer => "Deliver to the customer",
        DriverJobStep.Done => "Job completed",
        _ => "Job unavailable"
    };

    public static string StepHint(DriverJobStep step) => step switch
    {
        DriverJobStep.Accept => "Accept to lock in this job. You can release it until you collect the items.",
        DriverJobStep.CollectFromCustomer => "Count the items with the customer, then enter the pickup code shown in their app.",
        DriverJobStep.DropAtProvider => "Hand the bag over at the store. The provider confirms receipt in their app.",
        DriverJobStep.CollectFromProvider => "Check the package count and order number before you leave the store.",
        DriverJobStep.DeliverToCustomer => "Ask the customer for the delivery OTP shown in their app.",
        DriverJobStep.Done => "Nice work – earnings for this job have been added.",
        _ => "This job was taken by another driver, cancelled or put on hold."
    };

    public static string ActionText(DriverJobStep step) => step switch
    {
        DriverJobStep.CollectFromCustomer => "Navigate to Customer",
        DriverJobStep.DropAtProvider => "Navigate to Provider",
        DriverJobStep.CollectFromProvider => "Navigate to Provider",
        DriverJobStep.DeliverToCustomer => "Navigate to Customer",
        _ => string.Empty
    };
}
