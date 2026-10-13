namespace DryCleaning.Models;

/// <summary>Shared marketplace driver. Intentionally has NO ServiceProviderId.</summary>
public sealed class Driver : EntityBase
{
    public Guid UserId { get; set; }
    public Guid AddressId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
