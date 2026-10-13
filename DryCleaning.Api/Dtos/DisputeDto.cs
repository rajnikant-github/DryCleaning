using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class DisputeDto
{
    public required Guid Id { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required Guid RaisedByUserId { get; init; }
    public required string RaisedByName { get; init; }
    public required string Reason { get; init; }
    public required string? Description { get; init; }
    public required DisputeStatus Status { get; init; }
    public required string? Resolution { get; init; }
    public required DateTime CreatedAt { get; init; }
}
