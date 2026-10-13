using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>PUT api/driver/profile.</summary>
public sealed class UpdateDriverProfileRequest
{
    public string LicenseNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
