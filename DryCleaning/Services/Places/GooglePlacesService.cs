using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DryCleaning.Configuration;
using DryCleaning.Models;

namespace DryCleaning.Services.Places;

/// <summary>
/// Real address search with Google: Places API (New) for autocomplete and place details, and the
/// Geocoding API to turn the phone's location into an address. Uses a session token so one search
/// plus its details lookup is billed as a single session.
/// </summary>
public sealed class GooglePlacesService : IPlacesService
{
    private const string PlacesBase = "https://places.googleapis.com/v1/";
    private const string DetailsFields = "id,formattedAddress,addressComponents,location,plusCode";

    private readonly GoogleMapsSettings _settings;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private string _sessionToken = Guid.NewGuid().ToString();

    public GooglePlacesService(AppSettings settings)
    {
        _settings = settings.GoogleMaps;
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<IReadOnlyList<PlacePrediction>> AutocompleteAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
            return [];

        var body = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["input"] = query.Trim(),
            ["includedRegionCodes"] = new[] { _settings.RegionCode },
            ["languageCode"] = _settings.LanguageCode,
            ["sessionToken"] = _sessionToken
        }, PlacesJsonContext.Default.DictionaryStringObject);

        using var request = new HttpRequestMessage(HttpMethod.Post, PlacesBase + "places:autocomplete")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Goog-Api-Key", _settings.ApiKey);

        using var json = await SendAsync(request);
        if (!json.RootElement.TryGetProperty("suggestions", out var suggestions))
            return [];

        var results = new List<PlacePrediction>();
        foreach (var suggestion in suggestions.EnumerateArray())
        {
            if (!suggestion.TryGetProperty("placePrediction", out var p))
                continue;

            var main = Text(p, "structuredFormat", "mainText") ?? Text(p, "text") ?? string.Empty;
            results.Add(new PlacePrediction
            {
                PlaceId = p.GetProperty("placeId").GetString() ?? string.Empty,
                MainText = main,
                SecondaryText = Text(p, "structuredFormat", "secondaryText") ?? string.Empty
            });
        }

        return results;
    }

    public async Task<Address> GetDetailsAsync(string placeId)
    {
        var url = $"{PlacesBase}places/{Uri.EscapeDataString(placeId)}?languageCode={_settings.LanguageCode}&sessionToken={_sessionToken}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Goog-Api-Key", _settings.ApiKey);
        request.Headers.Add("X-Goog-FieldMask", DetailsFields);

        using var json = await SendAsync(request);
        _sessionToken = Guid.NewGuid().ToString(); // a details call ends the billing session

        var root = json.RootElement;
        var address = new Address
        {
            GooglePlaceId = root.TryGetProperty("id", out var id) ? id.GetString() : placeId,
            FormattedAddress = root.TryGetProperty("formattedAddress", out var formatted) ? formatted.GetString() ?? string.Empty : string.Empty,
            PlusCode = root.TryGetProperty("plusCode", out var plus) && plus.TryGetProperty("globalCode", out var global) ? global.GetString() : null
        };

        if (root.TryGetProperty("location", out var location))
        {
            address.Latitude = Coordinate(location, "latitude");
            address.Longitude = Coordinate(location, "longitude");
        }

        if (root.TryGetProperty("addressComponents", out var components))
        {
            foreach (var c in components.EnumerateArray())
                Apply(address, Types(c), Str(c, "longText"), Str(c, "shortText"));
        }

        return Finish(address);
    }

    private async Task<JsonDocument> SendAsync(HttpRequestMessage request)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException("Address search is unavailable right now. Check your connection and try again.", ex);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                // e.g. 403 when the API isn't enabled for this key, 400 for a bad request.
                Debug.WriteLine($"[Places] {request.RequestUri?.Host} returned {(int)response.StatusCode}: {text}");
                throw new InvalidOperationException("Address search isn't working right now. Please enter your address again in a moment.");
            }

            return JsonDocument.Parse(text);
        }
    }

    private static void Apply(Address a, IReadOnlyList<string> types, string? longText, string? shortText)
    {
        bool Is(string t) => types.Contains(t);

        if (Is("street_number")) a.StreetNumber = longText;
        else if (Is("route")) a.Route = longText;
        else if (Is("sublocality") || Is("sublocality_level_1") || (Is("neighborhood") && a.SubLocality is null)) a.SubLocality = longText;
        else if (Is("locality") || (Is("postal_town") && a.Locality is null)) a.Locality = longText;
        else if (Is("administrative_area_level_1")) a.AdministrativeAreaLevel1 = longText;
        else if (Is("administrative_area_level_2")) a.AdministrativeAreaLevel2 = longText;
        else if (Is("country")) { a.Country = longText; a.CountryCode = shortText; }
        else if (Is("postal_code")) a.PostalCode = longText;
        else if (Is("premise") || Is("subpremise")) a.AddressLine2 = longText;
    }

    private static Address Finish(Address a)
    {
        var line1 = string.Join(' ', new[] { a.StreetNumber, a.Route }.Where(s => !string.IsNullOrWhiteSpace(s)));
        a.AddressLine1 = string.IsNullOrWhiteSpace(line1) ? a.FormattedAddress.Split(',')[0].Trim() : line1;
        return a;
    }

    private static IReadOnlyList<string> Types(JsonElement component) =>
        component.TryGetProperty("types", out var types)
            ? types.EnumerateArray().Select(t => t.GetString() ?? string.Empty).ToList()
            : [];

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? Text(JsonElement e, params string[] path)
    {
        foreach (var part in path)
        {
            if (!e.TryGetProperty(part, out e))
                return null;
        }

        return Str(e, "text");
    }

    private static decimal Coordinate(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.TryGetDouble(out var d) ? Math.Round((decimal)d, 7) : 0m;
}
