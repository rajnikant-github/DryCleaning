using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ProviderSummaryDto
{
    public required Guid Id { get; init; }
    public required string BusinessName { get; init; }
    public required string? Description { get; init; }
    public required string? LogoUrl { get; init; }
    public required string? Locality { get; init; }
    public required IReadOnlyList<string> Categories { get; init; }
    public required decimal? StartingPrice { get; init; }

    /// <summary>Shortest EstimatedDays among the provider's active items (in the requested category, if any).</summary>
    public required int? FastestDays { get; init; }

    /// <summary>Average stars (1 decimal) from customers, or null before the first rating.</summary>
    public required double? RatingAverage { get; init; }
    public required int RatingCount { get; init; }
}
