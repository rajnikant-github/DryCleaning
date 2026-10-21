namespace DryCleaning.Models;

/// <summary>Where a provider or driver stands with the admin. Customers are always <see cref="Active"/>.</summary>
public enum ProfileStatus
{
    Active,

    /// <summary>Signed up and waiting for an admin to approve the account.</summary>
    PendingApproval,

    /// <summary>Was approved, then deactivated by an admin.</summary>
    Deactivated
}
