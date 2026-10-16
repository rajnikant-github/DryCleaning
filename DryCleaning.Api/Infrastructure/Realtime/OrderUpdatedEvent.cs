using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>An order's status, payment or driver jobs changed.</summary>
public sealed class OrderUpdatedEvent
{
    public required Guid OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required OrderStatus Status { get; init; }
}
