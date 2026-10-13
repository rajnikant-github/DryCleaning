using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/auth/register/driver.</summary>
public sealed class RegisterDriverRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
}
