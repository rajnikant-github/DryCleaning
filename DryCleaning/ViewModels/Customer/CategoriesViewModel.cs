using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C06 – Choose a category.</summary>
public sealed partial class CategoriesViewModel(ViewModelServices services, ICustomerCatalogService catalog, ICartService cart)
    : CartAwareViewModel(services, cart)
{
    public ObservableCollection<CategoryCardItem> Categories { get; } = [];

    protected override async Task<bool> LoadDataAsync()
    {
        var categories = await catalog.GetCategoriesAsync();
        Categories.Clear();
        foreach (var category in categories)
            Categories.Add(new CategoryCardItem(category, c => Navigation.GoToAsync(Routes.Providers, Param(Routes.CategoryIdKey, c.Id))));
        return Categories.Count > 0;
    }
}
