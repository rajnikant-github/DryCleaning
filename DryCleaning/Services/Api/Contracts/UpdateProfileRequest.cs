using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>PUT api/auth/me.</summary>
public sealed class UpdateProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
