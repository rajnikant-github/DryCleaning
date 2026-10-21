using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using DryCleaning.Api.Common;
using DryCleaning.Api.Domain;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Routing;

/// <summary>
/// Google Routes API (computeRoutes). Asks only for distance and duration with traffic-unaware routing, the cheapest
/// kind of request. Results are cached per pair of coordinates; a failed lookup is remembered briefly so a Google outage
/// doesn't make every quote wait.
/// </summary>
public sealed class GoogleRouteDistanceService(
    IHttpClientFactory httpFactory,
    IMemoryCache cache,
    IOptions<RoutesOptions> options,
    ILogger<GoogleRouteDistanceService> logger) : IRouteDistanceService
{
    public const string HttpClientName = "google-routes";
    private static readonly TimeSpan FailureMemory = TimeSpan.FromMinutes(5);

    public async Task<RouteResult?> GetAsync(Address from, Address to, CancellationToken ct)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
            return null;

        var key = $"route:{Coord(from.Latitude)},{Coord(from.Longitude)}>{Coord(to.Latitude)},{Coord(to.Longitude)}";
        if (cache.TryGetValue(key, out RouteResult? cached))
            return cached; // a stored null means "recently failed"

        RouteResult? result = null;
        try
        {
            result = await RequestAsync(from, to, settings.ApiKey!, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogWarning("Google Routes lookup failed: {Reason}", ex.Message);
        }

        cache.Set(key, result, result is null ? FailureMemory : TimeSpan.FromDays(Math.Max(1, settings.CacheDays)));
        return result;
    }

    private async Task<RouteResult?> RequestAsync(Address from, Address to, string apiKey, CancellationToken ct)
    {
        var http = httpFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "directions/v2:computeRoutes")
        {
            Content = JsonContent.Create(new
            {
                origin = Waypoint(from),
                destination = Waypoint(to),
                travelMode = "DRIVE",
                routingPreference = "TRAFFIC_UNAWARE"
            })
        };
        request.Headers.Add("X-Goog-Api-Key", apiKey);
        request.Headers.Add("X-Goog-FieldMask", "routes.distanceMeters,routes.duration");

        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Google Routes answered {Status}: {Body}", (int)response.StatusCode, Truncate(await response.Content.ReadAsStringAsync(ct)));
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
            return null; // no drivable route between the two points

        var route = routes[0];
        if (!route.TryGetProperty("distanceMeters", out var meters))
            return null;

        var seconds = route.TryGetProperty("duration", out var duration) &&
                      double.TryParse(duration.GetString()?.TrimEnd('s'), NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ? s : 0;
        return new RouteResult
        {
            DistanceKm = Math.Round(meters.GetInt32() / 1000m, 1),
            DurationMinutes = (int)Math.Ceiling(seconds / 60)
        };
    }

    private static object Waypoint(Address a) => new { location = new { latLng = new { latitude = (double)a.Latitude, longitude = (double)a.Longitude } } };

    private static string Coord(decimal value) => value.ToString("F5", CultureInfo.InvariantCulture);

    private static string Truncate(string text) => text.Length <= 300 ? text : text[..300];
}
