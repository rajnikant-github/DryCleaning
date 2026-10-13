using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

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
