using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class AdminCustomerDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string? Phone { get; init; }
    public required string? Email { get; init; }
    public required string? Address { get; init; }
    public required int OrderCount { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
}
