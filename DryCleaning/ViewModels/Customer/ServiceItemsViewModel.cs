using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C08 – Service Items of a product (Category → Product → Service Item).</summary>
public sealed partial class ServiceItemsViewModel(ViewModelServices services, ICustomerCatalogService catalog, ICartService cart)
    : CartAwareViewModel(services, cart), IQueryAttributable
{
    private Guid _productId;
    private Guid _providerId;

    public ObservableCollection<ServiceItemOption> Options { get; } = [];

    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private string productIcon = FontAwesome.BasketShopping;
    [ObservableProperty] private string productDescription = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private ServiceItemOption? selectedOption;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.ProductIdKey, out var productId))
            _productId = productId;
        if (TryGetGuid(query, Routes.ProviderIdKey, out var providerId))
            _providerId = providerId;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var product = await catalog.GetProductAsync(_productId);
        ProductName = product.Name;
        Title = product.Name;
        ProductIcon = CatalogIcons.ForProduct(product.Name);
        ProductDescription = product.Description ?? string.Empty;

        var previous = SelectedOption?.Item.Id;
        Options.Clear();
        foreach (var item in product.ServiceItems)
            Options.Add(new ServiceItemOption(item, Select) { IsSelected = item.Id == previous });
        SelectedOption = Options.FirstOrDefault(o => o.IsSelected);
        return Options.Count > 0;
    }

    private void Select(ServiceItemOption option)
    {
        foreach (var o in Options)
            o.IsSelected = ReferenceEquals(o, option);
        SelectedOption = option;
    }

    private bool CanContinue() => SelectedOption is not null;

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private Task ContinueAsync() =>
        Navigation.GoToAsync(Routes.ItemDetails, new Dictionary<string, object>
        {
            [Routes.ServiceItemIdKey] = SelectedOption!.Item.Id,
            [Routes.ProductIdKey] = _productId,
            [Routes.ProviderIdKey] = _providerId
        });
}
