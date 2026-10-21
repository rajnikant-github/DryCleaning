namespace DryCleaning.Api.Domain;

/// <summary>Shared marketplace driver. Intentionally has NO ServiceProviderId.</summary>
public class Driver : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? AddressId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>When an admin first approved the driver (null = never approved yet; older drivers are backfilled).</summary>
    public DateTime? ApprovedAt { get; set; }

    public User User { get; set; } = null!;
    public Address? Address { get; set; }
    public ICollection<DriverJob> Jobs { get; set; } = [];
}
