using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ActivityDto
{
    public required Guid OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required OrderStatus Status { get; init; }
    public required string? Notes { get; init; }
    public required DateTime At { get; init; }
}
