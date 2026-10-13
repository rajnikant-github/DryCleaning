using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class AdminOrderDto
{
    public required Guid Id { get; init; }
    public required string OrderNumber { get; init; }
    public required string CustomerName { get; init; }
    public required string ProviderName { get; init; }
    public required string? DriverName { get; init; }
    public required OrderStatus Status { get; init; }
    public required decimal TotalAmount { get; init; }
    public required DateTime CreatedAt { get; init; }
}
