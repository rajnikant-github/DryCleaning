using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class UserDto
{
    public required Guid Id { get; init; }
    public required UserRole Role { get; init; }
    public required string Name { get; init; }
    public required string? Email { get; init; }
    public required string? Phone { get; init; }
    public required Guid? ProfileId { get; init; }
    public required bool IsActive { get; init; }

    /// <summary>
    /// For providers and drivers: <c>PendingApproval</c> until an admin approves the account, <c>Deactivated</c> if it was approved and later switched off.
    /// Customers and admins: always <c>Active</c>.
    /// </summary>
    public ProfileStatus ProfileStatus { get; init; } = ProfileStatus.Active;
    public required string? PhotoUrl { get; init; }
}
