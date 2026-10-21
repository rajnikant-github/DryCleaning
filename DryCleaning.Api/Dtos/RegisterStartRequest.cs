using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>Step 1 of sign-up (all roles). Nothing is created yet: a code is emailed and the account is made when it is confirmed.</summary>
public sealed class RegisterStartRequest
{
    /// <summary><c>Customer</c>, <c>Provider</c> or <c>Driver</c>.</summary>
    public UserRole Role { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Required: the verification code is emailed here.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Optional contact number.</summary>
    public string? Phone { get; set; }

    public string Password { get; set; } = string.Empty;

    /// <summary>Provider only.</summary>
    public string? BusinessName { get; set; }

    /// <summary>Provider only.</summary>
    public string? Description { get; set; }

    /// <summary>Provider only.</summary>
    public IReadOnlyList<Guid>? CategoryIds { get; set; }

    /// <summary>Driver only.</summary>
    public string? LicenseNumber { get; set; }
}
