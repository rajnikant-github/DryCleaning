using DryCleaning.Models;

namespace DryCleaning.Services;

/// <summary>Google Places autocomplete / details. Sample Cape Town data until an API key is configured.</summary>
public interface IPlacesService
{
    Task<IReadOnlyList<PlacePrediction>> AutocompleteAsync(string query);
    Task<Address> GetDetailsAsync(string placeId);
}
