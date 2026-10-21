namespace DryCleaning.Api.Domain;

/// <summary>Where a provider or driver account stands with the admin. Customers and admins are always <see cref="Active"/>.</summary>
public enum ProfileStatus
{
    Active,

    /// <summary>Signed up, waiting for an admin to approve.</summary>
    PendingApproval,

    /// <summary>Was approved before, then deactivated by an admin.</summary>
    Deactivated
}
