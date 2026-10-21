namespace DryCleaning.Api.Services;

/// <summary>One extra charge as worked out for an order.</summary>
public sealed class PricedExtraCharge
{
    public required string Name { get; init; }
    public required decimal Amount { get; init; }

    /// <summary>Set for percentage charges.</summary>
    public decimal? Percent { get; init; }

    /// <summary>How it was worked out, e.g. "5% of items"; null for fixed amounts.</summary>
    public string? Basis { get; init; }

    public required bool IsTaxable { get; init; }
    public required int DisplayOrder { get; init; }
}

/// <summary>Every line of an order's price.</summary>
public sealed class PriceBreakdown
{
    public required decimal SubTotal { get; init; }
    public required decimal DeliveryFee { get; init; }
    public required IReadOnlyList<PricedExtraCharge> ExtraCharges { get; init; }
    public required decimal Tax { get; init; }

    /// <summary>The VAT percentage that was applied (0 when VAT is off).</summary>
    public required decimal VatRate { get; init; }

    public required decimal Total { get; init; }
}
