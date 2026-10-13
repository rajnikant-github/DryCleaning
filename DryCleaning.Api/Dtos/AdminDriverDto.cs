using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <remarks>No ServiceProviderId – drivers are shared marketplace drivers.</remarks>
public sealed class AdminDriverDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string? Phone { get; init; }
    public required string LicenseNumber { get; init; }
    public required bool IsAvailable { get; init; }
    public required int ActiveJobs { get; init; }
    public required int CompletedJobs { get; init; }
    public required bool IsActive { get; init; }
}
