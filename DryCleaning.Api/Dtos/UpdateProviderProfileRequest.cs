using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class UpdateProviderProfileRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
}
