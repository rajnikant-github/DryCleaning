using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>PUT api/provider/profile.</summary>
public sealed class UpdateProviderProfileRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
}
