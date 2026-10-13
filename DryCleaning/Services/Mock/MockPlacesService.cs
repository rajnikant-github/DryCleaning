using DryCleaning.Configuration;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

/// <summary>Sample Google Places results around Cape Town (until a Google API key is configured).</summary>
public sealed class MockPlacesService : IPlacesService
{
    private static readonly (string Id, string Number, string Route, string Suburb, string Postal, decimal Lat, decimal Lng)[] Samples =
    [
        ("ChIJ-sample-01", "12", "Regent Road", "Sea Point", "8005", -33.9180000m, 18.3870000m),
        ("ChIJ-sample-02", "8", "Kloof Street", "Gardens", "8001", -33.9310000m, 18.4120000m),
        ("ChIJ-sample-03", "27", "Main Road", "Claremont", "7708", -33.9800000m, 18.4650000m),
        ("ChIJ-sample-04", "3", "Lower Main Road", "Observatory", "7925", -33.9380000m, 18.4700000m),
        ("ChIJ-sample-05", "54", "Somerset Road", "Green Point", "8005", -33.9080000m, 18.4110000m),
        ("ChIJ-sample-06", "19", "Albert Road", "Woodstock", "7925", -33.9270000m, 18.4470000m),
        ("ChIJ-sample-07", "6", "Belmont Road", "Rondebosch", "7700", -33.9580000m, 18.4730000m),
        ("ChIJ-sample-08", "41", "Upper Orange Street", "Oranjezicht", "8001", -33.9400000m, 18.4150000m),
        ("ChIJ-sample-09", "15", "Beach Road", "Mouille Point", "8005", -33.9010000m, 18.4030000m),
        ("ChIJ-sample-10", "22", "Wale Street", "Bo-Kaap", "8001", -33.9210000m, 18.4140000m),
        ("ChIJ-sample-11", "100", "Long Street", "Cape Town City Centre", "8001", -33.9220000m, 18.4180000m),
        ("ChIJ-sample-12", "1", "Dock Road", "V&A Waterfront", "8001", -33.9040000m, 18.4200000m),
    ];

    public async Task<IReadOnlyList<PlacePrediction>> AutocompleteAsync(string query)
    {
        await Task.Delay(200);
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return [];

        var q = query.Trim();
        return Samples
            .Where(s => $"{s.Number} {s.Route} {s.Suburb} Cape Town".Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .Select(s => new PlacePrediction
            {
                PlaceId = s.Id,
                MainText = $"{s.Number} {s.Route}",
                SecondaryText = $"{s.Suburb}, Cape Town, South Africa"
            })
            .ToList();
    }

    public async Task<Address> GetDetailsAsync(string placeId)
    {
        await Task.Delay(200);
        var s = Samples.FirstOrDefault(x => x.Id == placeId);
        if (s.Id is null)
            throw new KeyNotFoundException("Place not found.");
        return ToAddress(s);
    }

    public async Task<Address?> GetCurrentLocationAddressAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                return null;

            var location = await Geolocation.Default.GetLastKnownLocationAsync()
                           ?? await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            if (location is null)
                return ToAddress(Samples[10]);

            // Nearest sample address (real reverse-geocoding comes with the Google API key).
            var nearest = Samples.OrderBy(s => Math.Pow((double)s.Lat - location.Latitude, 2) + Math.Pow((double)s.Lng - location.Longitude, 2)).First();
            return ToAddress(nearest);
        }
        catch (Exception)
        {
            return ToAddress(Samples[10]);
        }
    }

    private static Address ToAddress((string Id, string Number, string Route, string Suburb, string Postal, decimal Lat, decimal Lng) s) => new()
    {
        GooglePlaceId = s.Id,
        StreetNumber = s.Number,
        Route = s.Route,
        AddressLine1 = $"{s.Number} {s.Route}",
        SubLocality = s.Suburb,
        Locality = "Cape Town",
        AdministrativeAreaLevel1 = "Western Cape",
        AdministrativeAreaLevel2 = "City of Cape Town",
        Country = "South Africa",
        CountryCode = "ZA",
        PostalCode = s.Postal,
        Latitude = s.Lat,
        Longitude = s.Lng,
        PlusCode = null,
        FormattedAddress = $"{s.Number} {s.Route}, {s.Suburb}, Cape Town, {s.Postal}, South Africa"
    };
}
