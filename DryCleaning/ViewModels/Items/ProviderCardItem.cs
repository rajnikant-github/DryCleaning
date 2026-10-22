using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Items;

public sealed class ProviderCardItem(ProviderListing provider, Func<ProviderListing, Task> open)
{
    public string Name => provider.BusinessName;
    public string Initials => StringHelpers.Initials(provider.BusinessName);
    public string Description => provider.Description ?? string.Empty;
    public string LocalityText => string.IsNullOrEmpty(provider.Locality) ? "Cape Town" : provider.Locality;
    public string CategoriesText => string.Join(" · ", provider.Categories);
    public string StartingPriceText => provider.StartingPrice is { } price ? $"from {CurrencyFormatter.Current.Format(price)}" : string.Empty;
    public string TurnaroundText => provider.FastestDays switch
    {
        null => string.Empty,
        1 => "Next day available",
        var d => $"From {d} days"
    };
    public string RatingText => provider.RatingAverage is { } average && provider.RatingCount > 0
        ? $"  {average:0.0} ({provider.RatingCount})"
        : "  New";

    public IAsyncRelayCommand OpenCommand { get; } = new AsyncRelayCommand(() => open(provider));
}
