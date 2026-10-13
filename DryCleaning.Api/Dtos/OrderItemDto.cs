using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class OrderItemDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid ServiceItemId { get; init; }
    public required string CategoryName { get; init; }
    public required string ProductName { get; init; }
    public required string ServiceItemName { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalPrice { get; init; }
    public required string? Notes { get; init; }
    public required int EstimatedDays { get; init; }
}
