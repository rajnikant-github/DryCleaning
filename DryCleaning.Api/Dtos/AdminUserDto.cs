using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>A user account of any role, for pickers in the admin portal.</summary>
public sealed class AdminUserDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required UserRole Role { get; init; }
    public required string? Email { get; init; }
    public required string? Phone { get; init; }
    public required bool IsActive { get; init; }
}
