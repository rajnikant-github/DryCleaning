using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class PartyDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string? Phone { get; init; }
}
