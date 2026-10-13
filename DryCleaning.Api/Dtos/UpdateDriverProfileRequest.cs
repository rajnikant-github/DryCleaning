using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>Licence number is required; name, email and phone are updated when supplied.</summary>
public sealed class UpdateDriverProfileRequest
{
    public string LicenseNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
