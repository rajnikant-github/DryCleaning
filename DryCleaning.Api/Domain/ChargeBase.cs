namespace DryCleaning.Api.Domain;

/// <summary>What a percentage extra charge is a percentage of.</summary>
public enum ChargeBase
{
    /// <summary>The items subtotal only.</summary>
    ItemsSubtotal,

    /// <summary>The items subtotal plus the pickup &amp; delivery fee.</summary>
    ItemsAndDelivery
}
