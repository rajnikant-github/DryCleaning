using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/driver/profile.</summary>
public sealed class DriverProfileDto
{
    public Guid Id { get; set; }
    public UserDto User { get; set; } = new();
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
    public AddressDto? Address { get; set; }
}
