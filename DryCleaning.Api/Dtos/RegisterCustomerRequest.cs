using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

// Auth

public sealed class RegisterCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;
}
