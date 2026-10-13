using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class OrderSummaryDto
{
    public required Guid Id { get; init; }
    public required string OrderNumber { get; init; }
    public required OrderStatus Status { get; init; }
    public required string CustomerName { get; init; }
    public required string ProviderName { get; init; }
    public required int ItemCount { get; init; }
    public required string ItemsText { get; init; }
    public required decimal TotalAmount { get; init; }
    public required bool IsPaid { get; init; }
    public required bool HasOpenDispute { get; init; }
    public required DateTime? PickupDate { get; init; }
    public required string? PickupSlot { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
