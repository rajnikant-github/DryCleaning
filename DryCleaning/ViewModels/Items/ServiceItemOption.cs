using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Items;

public sealed partial class ServiceItemOption : ObservableObject
{
    public ServiceItemOption(ServiceItem item, Action<ServiceItemOption> select)
    {
        Item = item;
        SelectCommand = new RelayCommand(() => select(this));
    }

    public ServiceItem Item { get; }
    public string Name => Item.Name;
    public string Description => Item.Description;
    public decimal Price => Item.Price;
    public string EstimatedText => Item.EstimatedDays == 1 ? "Estimated: 1 day" : $"Estimated: {Item.EstimatedDays} days";
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool isSelected;
}
