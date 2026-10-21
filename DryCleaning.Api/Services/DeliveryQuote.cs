namespace DryCleaning.Api.Services;

/// <summary>The pickup &amp; delivery fee for one order and how it was worked out.</summary>
public sealed class DeliveryQuote
{
    public required decimal Fee { get; init; }

    /// <summary>Kilometres used for the fee; null when an address has no location (the base fee applies).</summary>
    public decimal? DistanceKm { get; init; }

    public int? DurationMinutes { get; init; }

    /// <summary>True when the distance is a straight-line estimate because Google couldn't measure the road distance.</summary>
    public bool IsEstimate { get; init; }
}
