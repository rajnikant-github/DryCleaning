using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ProviderProfileDto
{
    public required Guid Id { get; init; }
    public required string BusinessName { get; init; }
    public required string? Description { get; init; }
    public required string? Phone { get; init; }
    public required string? Email { get; init; }
    public required string? LogoUrl { get; init; }
    public required bool IsActive { get; init; }
    public required UserDto Owner { get; init; }
    public required AddressDto? Address { get; init; }
    public required IReadOnlyList<CategoryDto> Categories { get; init; }
}
