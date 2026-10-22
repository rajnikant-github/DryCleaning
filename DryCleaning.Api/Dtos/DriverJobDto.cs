using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class DriverJobDto
{
    public required Guid Id { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required DriverJobType JobType { get; init; }
    public required DriverJobStatus Status { get; init; }
    public required Guid? DriverId { get; init; }
    public required string? DriverName { get; init; }
    public required string? DriverPhone { get; init; }
    public required string? DriverPhotoUrl { get; init; }
    public required string PickupName { get; init; }
    public required AddressDto PickupAddress { get; init; }
    public required string DeliveryName { get; init; }
    public required AddressDto DeliveryAddress { get; init; }
    public required int ItemCount { get; init; }
    public required decimal EstimatedEarnings { get; init; }
    public required DateTime? AssignedAt { get; init; }
    public required DateTime? CompletedAt { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required OrderStatus OrderStatus { get; init; }
    public required DateTime? PickupDate { get; init; }
    public required string? PickupSlot { get; init; }

    /// <summary>The customer's note for the driver (leave at the gate, etc.).</summary>
    public required string? DriverNotes { get; init; }
    public required bool IsOnHold { get; init; }
}
