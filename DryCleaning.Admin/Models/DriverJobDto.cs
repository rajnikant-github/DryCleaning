namespace DryCleaning.Admin.Models;

public sealed class DriverJobDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DriverJobType JobType { get; set; }
    public DriverJobStatus Status { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string PickupName { get; set; } = string.Empty;
    public AddressDto PickupAddress { get; set; } = new();
    public string DeliveryName { get; set; } = string.Empty;
    public AddressDto DeliveryAddress { get; set; } = new();
    public int ItemCount { get; set; }
    public decimal EstimatedEarnings { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public DateTime? PickupDate { get; set; }
    public string? PickupSlot { get; set; }
    public bool IsOnHold { get; set; }
}
