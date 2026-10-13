using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

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
