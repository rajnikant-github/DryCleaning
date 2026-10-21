using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class AdminProviderDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string OwnerName { get; init; }
    public required string BusinessName { get; init; }
    public required IReadOnlyList<string> Categories { get; init; }
    public required int OrderCount { get; init; }
    public required bool IsActive { get; init; }

    /// <summary><c>PendingApproval</c> for a new business, <c>Deactivated</c> if it was approved and later switched off.</summary>
    public ProfileStatus ProfileStatus { get; init; }
    public required DateTime CreatedAt { get; init; }
}
