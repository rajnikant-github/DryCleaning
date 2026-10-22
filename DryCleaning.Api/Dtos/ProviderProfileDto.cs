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
    public required double? RatingAverage { get; init; }
    public required int RatingCount { get; init; }

    /// <summary>The newest ratings that have a visible comment.</summary>
    public required IReadOnlyList<RatingDto> LatestRatings { get; init; }
}
