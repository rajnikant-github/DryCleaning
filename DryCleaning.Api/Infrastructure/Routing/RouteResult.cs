namespace DryCleaning.Api.Infrastructure.Routing;

/// <summary>A measured driving route.</summary>
public sealed class RouteResult
{
    public required decimal DistanceKm { get; init; }
    public required int DurationMinutes { get; init; }
}
