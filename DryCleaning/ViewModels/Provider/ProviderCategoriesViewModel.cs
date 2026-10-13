using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Switch the marketplace categories this provider offers on or off.</summary>
public sealed partial class ProviderCategoriesViewModel(ViewModelServices services, IProviderProfileService profiles)
    : BaseViewModel(services)
{
    public ObservableCollection<CategoryToggleItem> Categories { get; } = [];

    protected override async Task<bool> LoadDataAsync()
    {
        var options = await profiles.GetCategoryOptionsAsync();
        Categories.Clear();
        foreach (var option in options)
            Categories.Add(new CategoryToggleItem(option.CategoryId, option.Name, option.Description, option.IsEnabled));
        return Categories.Count > 0;
    }

    // Load once – keep the user's switches when the page re-appears.
    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    [RelayCommand]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        var selected = Categories.Where(c => c.IsSelected).Select(c => c.Id).ToList();
        if (selected.Count == 0)
        {
            await Dialogs.AlertAsync("Categories", "Choose at least one category you offer.");
            return;
        }

        await profiles.SetCategoriesAsync(selected);
        await Navigation.GoBackAsync();
    });
}
