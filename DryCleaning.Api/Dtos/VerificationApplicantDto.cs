using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>Everything the admin needs to review one provider or driver.</summary>
public sealed class VerificationApplicantDto
{
    public required Guid UserId { get; init; }
    public required Guid? ProfileId { get; init; }
    public required string Name { get; init; }
    public required string? Email { get; init; }
    public required string? Phone { get; init; }
    public required UserRole Role { get; init; }
    public required string? BusinessName { get; init; }
    public required string? LicenseNumber { get; init; }
    public required ProfileStatus ProfileStatus { get; init; }
    public required IReadOnlyList<ReviewDocumentDto> Documents { get; init; }
    public required int RequiredCount { get; init; }
    public required int VerifiedRequiredCount { get; init; }

    /// <summary>True when every required document is verified, so the account can be approved.</summary>
    public required bool CanApprove { get; init; }
}
