namespace DryCleaning.Api.Domain;

/// <summary>
/// An extra charge the admin adds to every order (for example a service fee). A fixed amount, switched on or off in the admin portal.
/// Orders copy the charges that were on when they were placed (<see cref="OrderExtraCharge"/>), so editing or deleting one never changes past orders.
/// </summary>
public class ExtraCharge : EntityBase
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Short explanation for the admin (and the customer, if shown).</summary>
    public string? Description { get; set; }

    public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Lowest shows first on the summary.</summary>
    public int DisplayOrder { get; set; }
}
