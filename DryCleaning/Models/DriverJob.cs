namespace DryCleaning.Models;

public sealed class DriverJob : EntityBase
{
    public Guid OrderId { get; set; }

    /// <summary>Null while the job is Pending on the marketplace.</summary>
    public Guid? DriverId { get; set; }
    public DriverJobType JobType { get; set; }
    public DriverJobStatus Status { get; set; }
    public Guid PickupAddressId { get; set; }
    public Guid DeliveryAddressId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
