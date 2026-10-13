using DryCleaning.Models;

namespace DryCleaning.Helpers;

/// <summary>Straight-line distance and rough city ETA until a maps/directions API is wired in.</summary>
public static class Geo
{
    public static double DistanceKm(Address from, Address to)
    {
        const double earthRadiusKm = 6371;
        double Rad(decimal degrees) => (double)degrees * Math.PI / 180;

        var dLat = Rad(to.Latitude - from.Latitude);
        var dLng = Rad(to.Longitude - from.Longitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(from.Latitude)) * Math.Cos(Rad(to.Latitude)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var straight = earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(straight * 1.3, 1); // roads are ~30% longer than a straight line
    }

    /// <summary>Approximate driving time at ~25 km/h average city speed (min 5 minutes).</summary>
    public static int EtaMinutes(double km) => Math.Max(5, (int)Math.Round(km / 25 * 60));

    public static string DistanceText(double km) => km < 1 ? $"{km * 1000:0} m" : $"{km:0.0} km";
}
