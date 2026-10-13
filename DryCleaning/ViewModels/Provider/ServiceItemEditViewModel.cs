using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Add / edit a Service Item (price, estimated days, enable/disable).</summary>
public sealed partial class ServiceItemEditViewModel(ViewModelServices services, IProviderCatalogService catalog, ICurrencyFormatter currency)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid _productId;
    private Guid _serviceItemId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string name = string.Empty;

    [ObservableProperty] private string description = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string priceText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstimatedDaysText))]
    private int estimatedDays = 2;

    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isNew = true;
    [ObservableProperty] private string productName = string.Empty;

    public string CurrencySymbol => currency.Symbol;
    public string CurrencyCode => currency.Code;
    public string EstimatedDaysText => EstimatedDays == 1 ? "1 day" : $"{EstimatedDays} days";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.ProductIdKey, out var productId))
            _productId = productId;
        if (TryGetGuid(query, Routes.ServiceItemIdKey, out var itemId))
            _serviceItemId = itemId;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var product = await catalog.GetProductAsync(_productId);
        ProductName = product.Name;
        IsNew = _serviceItemId == Guid.Empty;
        Title = IsNew ? "Add Service Item" : "Edit Service Item";

        if (!IsNew)
        {
            var item = await catalog.GetServiceItemAsync(_serviceItemId);
            Name = item.Name;
            Description = item.Description;
            PriceText = item.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            EstimatedDays = item.EstimatedDays;
            IsActive = item.IsActive;
        }

        return true;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(PriceText);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        if (!currency.TryParse(PriceText, out var price) || price <= 0)
        {
            await Dialogs.AlertAsync("Invalid price", $"Enter a price greater than 0 in {currency.Code}.");
            return;
        }

        await catalog.SaveServiceItemAsync(new ServiceItem
        {
            Id = IsNew ? Guid.NewGuid() : _serviceItemId,
            ProductId = _productId,
            Name = Name.Trim(),
            Description = Description.Trim(),
            Price = Math.Round(price, 2),
            EstimatedDays = Math.Max(1, EstimatedDays),
            IsActive = IsActive
        });

        await Navigation.GoBackAsync();
    });
}
