namespace DryCleaning.Api.Domain;

public static class ProfileStatusExtensions
{
    /// <summary>Works out the status from the account's active flag and when it was first approved.</summary>
    public static ProfileStatus StatusOf(bool isActive, DateTime? approvedAt) =>
        isActive ? ProfileStatus.Active : approvedAt is null ? ProfileStatus.PendingApproval : ProfileStatus.Deactivated;
}
