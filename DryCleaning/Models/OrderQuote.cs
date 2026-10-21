namespace DryCleaning.Models;

/// <summary>The price of an order as the server works it out: items, VAT and the pickup &amp; delivery fee for the real distance.</summary>
public sealed class OrderQuote
{
    public decimal SubTotal { get; init; }
    public decimal Tax { get; init; }
    public decimal DeliveryFee { get; init; }
    public decimal Total { get; init; }

    /// <summary>Driving distance between the pickup address and the provider; null when unknown.</summary>
    public decimal? DistanceKm { get; init; }

    /// <summary>True when the distance is only an estimate (the road distance couldn't be measured).</summary>
    public bool DistanceIsEstimate { get; init; }

    public int? DurationMinutes { get; init; }
}
