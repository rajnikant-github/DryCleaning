using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Routing;

public interface IRouteDistanceService
{
    /// <summary>Driving distance and time between two addresses, or null when it can't be measured (not configured, Google unreachable).</summary>
    Task<RouteResult?> GetAsync(Address from, Address to, CancellationToken ct);
}
