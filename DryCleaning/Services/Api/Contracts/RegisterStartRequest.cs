namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/auth/register/start: nothing is created until the emailed code is confirmed.</summary>
public sealed class RegisterStartRequest
{
    /// <summary>Customer, Provider or Driver.</summary>
    public string Role { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Password { get; set; } = string.Empty;

    public string? BusinessName { get; set; }
    public string? Description { get; set; }
    public List<Guid>? CategoryIds { get; set; }
    public string? LicenseNumber { get; set; }
}
