using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C11 – Pickup address via Google Places search (one address per role in V1).</summary>
public sealed partial class PickupAddressViewModel(
    ViewModelServices services, IPlacesService places, ICustomerProfileService profiles, ICartService cart,
    IProviderProfileService providerProfiles)
    : BaseViewModel(services), IQueryAttributable
{
    /// <summary>Mode value used when a service provider edits the business address from their Profile.</summary>
    public const string ProviderMode = "provider";

    private string _mode = "checkout";

    private bool IsProviderMode => _mode == ProviderMode;
    private CancellationTokenSource? _searchCts;

    public ObservableCollection<PredictionItem> Predictions { get; } = [];
    public ObservableCollection<KeyValueRow> AddressFields { get; } = [];

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isSearching;
    [ObservableProperty] private bool hasPredictions;
    [ObservableProperty] private bool showNoResults;
    [ObservableProperty] private string searchError = string.Empty;
    [ObservableProperty] private bool hasSearchError;
    [ObservableProperty] private string addressLine2 = string.Empty;
    [ObservableProperty] private bool showDetails;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(FormattedAddress))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private Address? selected;

    public bool HasSelection => Selected is not null;
    public string FormattedAddress => Selected?.FormattedAddress ?? string.Empty;
    public string ConfirmText => _mode == "checkout" ? "Confirm Address" : "Save Address";
    public string PageTitle => IsProviderMode ? "Business Address" : "Pickup Address";
    public string HeadlineText => IsProviderMode ? "Where is your business?" : "Where should we collect?";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.ModeKey, out var mode) && mode is string m)
            _mode = m;
        OnPropertyChanged(nameof(ConfirmText));
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(HeadlineText));
    }

    protected override async Task<bool> LoadDataAsync()
    {
        if (Selected is null)
        {
            Address? current;
            if (IsProviderMode)
            {
                var business = (await providerProfiles.GetProfileAsync()).Address;
                current = string.IsNullOrWhiteSpace(business.FormattedAddress) ? null : business;
            }
            else
            {
                current = cart.PickupAddress ?? (await profiles.GetProfileAsync()).Address;
            }

            if (current is not null)
                SetSelected(current);
        }

        return true;
    }

    partial void OnSearchTextChanged(string value) => _ = SearchAsync(value);

    private async Task SearchAsync(string query)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(300, token); // debounce typing
            IsSearching = true;
            var results = await places.AutocompleteAsync(query);
            if (token.IsCancellationRequested)
                return;

            HasSearchError = false;
            Predictions.Clear();
            foreach (var prediction in results)
                Predictions.Add(new PredictionItem(prediction, SelectPredictionAsync));
            HasPredictions = Predictions.Count > 0;
            ShowNoResults = !HasPredictions && query.Trim().Length >= 2;
        }
        catch (TaskCanceledException)
        {
            // newer keystroke
        }
        catch (Exception ex)
        {
            // Shown under the search box (not a pop-up) because this runs while the user types.
            if (!token.IsCancellationRequested)
            {
                Predictions.Clear();
                HasPredictions = false;
                ShowNoResults = false;
                SearchError = ex.Message;
                HasSearchError = true;
            }
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsSearching = false;
        }
    }

    private async Task SelectPredictionAsync(PlacePrediction prediction)
    {
        try
        {
            IsBusy = true;
            var address = await places.GetDetailsAsync(prediction.PlaceId);
            SetSelected(address);
            _searchCts?.Cancel();
            SearchText = string.Empty;
            Predictions.Clear();
            HasPredictions = false;
            ShowNoResults = false;
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync("Address", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetSelected(Address address)
    {
        Selected = address;
        AddressLine2 = address.AddressLine2 ?? string.Empty;
        AddressFields.Clear();
        void Row(string label, object? value)
        {
            var text = value?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                AddressFields.Add(new KeyValueRow { Label = label, Value = text });
        }

        Row("Google Place ID", address.GooglePlaceId);
        Row("Address line 1", address.AddressLine1);
        Row("Street number", address.StreetNumber);
        Row("Route", address.Route);
        Row("Sub-locality", address.SubLocality);
        Row("Locality", address.Locality);
        Row("Province", address.AdministrativeAreaLevel1);
        Row("District", address.AdministrativeAreaLevel2);
        Row("Country", address.Country);
        Row("Country code", address.CountryCode);
        Row("Postal code", address.PostalCode);
        Row("Latitude", address.Latitude.ToString("0.0000000"));
        Row("Longitude", address.Longitude.ToString("0.0000000"));
        Row("Plus code", address.PlusCode);
    }

    [RelayCommand]
    private void ToggleDetails() => ShowDetails = !ShowDetails;

    private bool CanConfirm() => Selected is not null;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private Task ConfirmAsync() => RunBusyAsync(async () =>
    {
        var address = Selected!;
        address.AddressLine2 = string.IsNullOrWhiteSpace(AddressLine2) ? null : AddressLine2.Trim();
        if (IsProviderMode)
        {
            await providerProfiles.SaveAddressAsync(address);
            await Navigation.GoBackAsync();
            return;
        }

        var saved = await profiles.SaveAddressAsync(address);
        cart.PickupAddress = saved;

        if (_mode == "checkout")
            await Navigation.GoToAsync(Routes.PickupTime);
        else
            await Navigation.GoBackAsync();
    });
}
