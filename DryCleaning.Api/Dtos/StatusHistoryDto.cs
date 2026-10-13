using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class StatusHistoryDto
{
    public required OrderStatus Status { get; init; }
    public required string? Notes { get; init; }
    public required Guid? ChangedByUserId { get; init; }
    public required DateTime CreatedAt { get; init; }
}
