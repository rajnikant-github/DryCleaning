using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

// Item ViewModels carry their own commands so DataTemplates can use compiled
// bindings without RelativeSource lookups back to the page ViewModel.

public sealed class OrderCardItem
{
    public OrderCardItem(
        OrderSummary summary,
        Func<OrderSummary, Task> open,
        Func<OrderSummary, Task>? accept = null,
        Func<OrderSummary, Task>? reject = null,
        Func<OrderSummary, Task>? primaryAction = null,
        string? title = null,
        string? hint = null)
    {
        Summary = summary;
        Title = title ?? summary.CustomerName;
        OpenCommand = new AsyncRelayCommand(() => open(summary));
        AcceptCommand = new AsyncRelayCommand(() => accept?.Invoke(summary) ?? Task.CompletedTask);
        RejectCommand = new AsyncRelayCommand(() => reject?.Invoke(summary) ?? Task.CompletedTask);
        PrimaryActionCommand = new AsyncRelayCommand(() => primaryAction?.Invoke(summary) ?? Task.CompletedTask);

        ShowAcceptReject = accept is not null && summary.Status == OrderStatus.Created;
        var action = ProviderActions.ForStatus(summary.Status);
        HasPrimaryAction = !ShowAcceptReject && primaryAction is not null && action is not null;
        PrimaryActionText = action?.Label ?? string.Empty;
        HintText = hint ?? (summary.HasOpenDispute && summary.Status != OrderStatus.Disputed
            ? "⚠ Issue reported"
            : ProviderActions.HintFor(summary.Status) ?? string.Empty);
    }

    public OrderSummary Summary { get; }

    public string OrderNumber => Summary.OrderNumber;
    public string CustomerName => Summary.CustomerName;

    /// <summary>Card heading: customer name (provider app) or provider name (customer app).</summary>
    public string Title { get; }
    public string Subtitle => $"{Summary.OrderNumber} · {Summary.ItemsText}";
    public string ItemCountText => Summary.ItemCount == 1 ? "1 item" : $"{Summary.ItemCount} items";
    public decimal TotalAmount => Summary.TotalAmount;
    public OrderStatus Status => Summary.Status;
    public string TimeText => TimeFormat.Relative(Summary.UpdatedAt);

    public string HintText { get; }
    public bool HasHint => !string.IsNullOrEmpty(HintText);
    public bool ShowAcceptReject { get; }
    public bool HasPrimaryAction { get; }
    public string PrimaryActionText { get; }
    public bool HasActions => ShowAcceptReject || HasPrimaryAction;

    public IAsyncRelayCommand OpenCommand { get; }
    public IAsyncRelayCommand AcceptCommand { get; }
    public IAsyncRelayCommand RejectCommand { get; }
    public IAsyncRelayCommand PrimaryActionCommand { get; }
}

public sealed partial class SegmentItem : ObservableObject
{
    public SegmentItem(string key, string title, Action<SegmentItem> onSelected)
    {
        Key = key;
        Title = title;
        SelectCommand = new RelayCommand(() => onSelected(this));
    }

    public string Key { get; }
    public string Title { get; }
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCount))]
    private int count;

    public bool HasCount => Count > 0;
}

public sealed partial class ChecklistItem : ObservableObject
{
    private readonly Action? _changed;

    public ChecklistItem(string text, Action? changed = null)
    {
        Text = text;
        _changed = changed;
        ToggleCommand = new RelayCommand(() => IsChecked = !IsChecked);
    }

    public string Text { get; }
    public IRelayCommand ToggleCommand { get; }

    [ObservableProperty]
    private bool isChecked;

    partial void OnIsCheckedChanged(bool value) => _changed?.Invoke();
}

public sealed class TimelineStep
{
    public string Title { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool IsDone { get; init; }
    public bool IsActive { get; init; }
    public bool IsAlert { get; init; }
    public bool IsLast { get; init; }

    public bool HasTime => !string.IsNullOrEmpty(TimeText);
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public bool ShowLine => !IsLast;
    public string Marker => IsAlert ? "!" : IsDone ? "✓" : string.Empty;
}

public sealed class ActivityItem
{
    public ActivityItem(ActivityEntry entry, Func<Guid, Task> open)
    {
        Entry = entry;
        OpenCommand = new AsyncRelayCommand(() => open(entry.OrderId));
    }

    public ActivityEntry Entry { get; }
    public string Text => Entry.Text;
    public string TimeText => TimeFormat.Relative(Entry.At);
    public OrderStatus Status => Entry.Status;
    public IAsyncRelayCommand OpenCommand { get; }
}

public sealed class NotificationItem
{
    public NotificationItem(Notification notification, Func<NotificationItem, Task> open)
    {
        Notification = notification;
        OpenCommand = new AsyncRelayCommand(() => open(this));
    }

    public Notification Notification { get; }
    public string Icon => CatalogIcons.ForNotification(Notification.Type);
    public string Title => Notification.Title;
    public string Message => Notification.Message;
    public string TimeText => TimeFormat.Relative(Notification.CreatedAt);
    public bool IsUnread => !Notification.IsRead;
    public IAsyncRelayCommand OpenCommand { get; }
}

public sealed partial class ProductCardItem : ObservableObject
{
    private readonly Func<ProductCardItem, bool, Task> _setActive;
    private bool _suppress;

    public ProductCardItem(ProductSummary summary, Func<ProductCardItem, Task> open, Func<ProductCardItem, bool, Task> setActive)
    {
        Summary = summary;
        _setActive = setActive;
        _suppress = true;
        IsActive = summary.Product.IsActive;
        _suppress = false;
        OpenCommand = new AsyncRelayCommand(() => open(this));
    }

    public ProductSummary Summary { get; }
    public Guid Id => Summary.Product.Id;
    public string Icon => CatalogIcons.ForProduct(Summary.Product.Name);
    public string Name => Summary.Product.Name;
    public string Description => Summary.Product.Description;
    public string ServiceItemsText => Summary.ServiceItemCount == 1 ? "1 service item" : $"{Summary.ServiceItemCount} service items";
    public string StartingPriceText => Summary.StartingPrice is { } price
        ? $"From {CurrencyFormatter.Current.Format(price)}"
        : "No active service items";
    public IAsyncRelayCommand OpenCommand { get; }

    [ObservableProperty]
    private bool isActive;

    partial void OnIsActiveChanged(bool value)
    {
        if (!_suppress)
            _ = _setActive(this, value);
    }

    public void SetActiveSilently(bool value)
    {
        _suppress = true;
        IsActive = value;
        _suppress = false;
    }
}

public sealed partial class ServiceItemRow : ObservableObject
{
    private readonly Func<ServiceItemRow, bool, Task> _setActive;
    private bool _suppress;

    public ServiceItemRow(ServiceItem item, Func<ServiceItemRow, Task> edit, Func<ServiceItemRow, bool, Task> setActive)
    {
        Item = item;
        _setActive = setActive;
        _suppress = true;
        IsActive = item.IsActive;
        _suppress = false;
        EditCommand = new AsyncRelayCommand(() => edit(this));
    }

    public ServiceItem Item { get; }
    public string Name => Item.Name;
    public string Description => Item.Description;
    public decimal Price => Item.Price;
    public string EstimatedText => Item.EstimatedDays == 1 ? "Est. 1 day" : $"Est. {Item.EstimatedDays} days";
    public IAsyncRelayCommand EditCommand { get; }

    [ObservableProperty]
    private bool isActive;

    partial void OnIsActiveChanged(bool value)
    {
        if (!_suppress)
            _ = _setActive(this, value);
    }

    public void SetActiveSilently(bool value)
    {
        _suppress = true;
        IsActive = value;
        _suppress = false;
    }
}

/// <summary>Per-item state on the Inspection screen (P06).</summary>
public sealed partial class InspectionItemViewModel : ObservableObject
{
    private readonly Func<Task<string?>> _addPhoto;
    private readonly Func<InspectionItemViewModel, string, Task> _report;
    private readonly Action _changed;

    public InspectionItemViewModel(
        OrderItemDetail item,
        Func<Task<string?>> addPhoto,
        Func<InspectionItemViewModel, string, Task> report,
        Action changed)
    {
        Item = item;
        _addPhoto = addPhoto;
        _report = report;
        _changed = changed;
        receivedQuantity = item.Quantity;
    }

    public OrderItemDetail Item { get; }
    public string Icon => Item.Icon;
    public string ProductName => Item.ProductName;
    public string ServiceItemName => Item.ServiceItemName;
    public string CategoryName => Item.CategoryName;
    public int ExpectedQuantity => Item.Quantity;
    public string ExpectedText => $"Expected: {Item.Quantity} pcs · {Item.ServiceItemName}";
    public System.Collections.ObjectModel.ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuantityDiscrepancy))]
    [NotifyPropertyChangedFor(nameof(QuantityDiscrepancyText))]
    private int receivedQuantity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGood))]
    [NotifyPropertyChangedFor(nameof(IsStained))]
    [NotifyPropertyChangedFor(nameof(IsDamaged))]
    [NotifyPropertyChangedFor(nameof(NeedsNote))]
    [NotifyPropertyChangedFor(nameof(NotePlaceholder))]
    private ItemCondition condition = ItemCondition.Good;

    [ObservableProperty]
    private string note = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReport))]
    private bool issueReported;

    public bool IsGood => Condition == ItemCondition.Good;
    public bool IsStained => Condition == ItemCondition.Stained;
    public bool IsDamaged => Condition == ItemCondition.Damaged;
    public bool NeedsNote => Condition != ItemCondition.Good;
    public string NotePlaceholder => IsStained ? "Describe stain location & type…" : "Describe the damage…";
    public bool HasQuantityDiscrepancy => ReceivedQuantity != ExpectedQuantity;
    public string QuantityDiscrepancyText => ReceivedQuantity < ExpectedQuantity
        ? $"Missing {ExpectedQuantity - ReceivedQuantity}"
        : $"Extra {ReceivedQuantity - ExpectedQuantity}";
    public bool CanReport => !IssueReported && (IsDamaged || HasQuantityDiscrepancy);

    partial void OnReceivedQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(CanReport));
        _changed();
    }

    partial void OnConditionChanged(ItemCondition value) => OnPropertyChanged(nameof(CanReport));

    [RelayCommand]
    private void SetCondition(string value)
    {
        if (Enum.TryParse<ItemCondition>(value, out var parsed))
            Condition = parsed;
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await _addPhoto();
        if (path is not null)
            Photos.Add(path);
    }

    [RelayCommand]
    private Task ReportDamageAsync() => _report(this, "Damage");

    [RelayCommand]
    private Task ReportQuantityAsync() => _report(this, "Quantity discrepancy");

    public InspectionResult ToResult() => new()
    {
        OrderItemId = Item.Id,
        ItemLabel = $"{Item.ProductName} ({Item.ServiceItemName})",
        ExpectedQuantity = ExpectedQuantity,
        ReceivedQuantity = ReceivedQuantity,
        Condition = Condition,
        Note = Note,
        PhotoPaths = Photos.ToList()
    };
}

/// <summary>Builds the order timeline (happy path, or reached steps + terminal status).</summary>
public static class TimelineBuilder
{
    public static List<TimelineStep> Build(OrderDetail detail, Func<OrderStatus, string> title)
    {
        var status = detail.Order.Status;
        var reached = detail.History.Select(h => h.Status).ToHashSet();
        var terminal = status is OrderStatus.Cancelled or OrderStatus.Disputed;

        List<OrderStatus> steps;
        if (terminal)
        {
            steps = OrderWorkflow.Lifecycle.Where(reached.Contains).ToList();
            steps.Add(status);
        }
        else
        {
            steps = OrderWorkflow.Lifecycle.ToList();
        }

        var currentIndex = steps.IndexOf(status);
        var result = new List<TimelineStep>();
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var history = detail.History.LastOrDefault(h => h.Status == step);
            var isCurrent = i == currentIndex;
            result.Add(new TimelineStep
            {
                Title = title(step),
                TimeText = history is null ? string.Empty : history.CreatedAt.ToString("d MMM, HH:mm"),
                Notes = isCurrent || terminal ? history?.Notes : null,
                IsDone = i < currentIndex || (isCurrent && step == OrderStatus.Completed),
                IsActive = isCurrent && !terminal && step != OrderStatus.Completed,
                IsAlert = isCurrent && terminal,
                IsLast = i == steps.Count - 1
            });
        }

        return result;
    }
}

/// <summary>A label/value row (e.g. address fields).</summary>
public sealed record KeyValueRow(string Label, string Value);
