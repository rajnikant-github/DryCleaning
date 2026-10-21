namespace DryCleaning.Api.Domain;

/// <summary>How an extra charge's amount is worked out.</summary>
public enum ExtraChargeType
{
    /// <summary>A fixed amount (e.g. R10.00) added to every order.</summary>
    Fixed,

    /// <summary>A percentage of the order's items (optionally plus the pickup &amp; delivery fee).</summary>
    Percentage
}
