using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class VerificationQueueItemDto
{
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string? Email { get; init; }
    public required UserRole Role { get; init; }

    /// <summary>The business name (providers) or licence number (drivers).</summary>
    public required string? Detail { get; init; }

    public required ProfileStatus ProfileStatus { get; init; }
    public required int PendingCount { get; init; }
    public required int VerifiedCount { get; init; }
    public required int RejectedCount { get; init; }

    /// <summary>Required documents that are not verified yet.</summary>
    public required int RequiredRemaining { get; init; }

    public required DateTime? LastSubmittedAt { get; init; }
}
