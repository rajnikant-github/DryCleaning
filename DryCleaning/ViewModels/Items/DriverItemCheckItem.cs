using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

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
