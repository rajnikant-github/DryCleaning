using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class RegisterProviderRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<Guid>? CategoryIds { get; set; }
}
