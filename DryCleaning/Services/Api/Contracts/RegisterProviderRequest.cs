using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/auth/register/provider.</summary>
public sealed class RegisterProviderRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<Guid>? CategoryIds { get; set; }
}
