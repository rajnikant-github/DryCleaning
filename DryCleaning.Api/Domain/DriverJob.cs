namespace DryCleaning.Api.Domain;

public class DriverJob : EntityBase
{
    public Guid OrderId { get; set; }

    /// <summary>Null while the job is Pending on the marketplace.</summary>
    public Guid? DriverId { get; set; }
    public DriverJobType JobType { get; set; }
    public DriverJobStatus Status { get; set; } = DriverJobStatus.Pending;
    public Guid PickupAddressId { get; set; }
    public Guid DeliveryAddressId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>What the driver is paid for this job, fixed when the job is created from the admin portal's driver pay setting.</summary>
    public decimal Earning { get; set; }

    public Order Order { get; set; } = null!;
    public Driver? Driver { get; set; }
    public Address PickupAddress { get; set; } = null!;
    public Address DeliveryAddress { get; set; } = null!;
}
