using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class UpdateProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
