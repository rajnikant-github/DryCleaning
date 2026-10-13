namespace DryCleaning.Models;

/// <summary>What the Sign-up screen collects. Role-specific fields are ignored for other roles.</summary>
public sealed class Registration
{
    public UserRole Role { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string Password { get; init; } = string.Empty;

    /// <summary>Driver only.</summary>
    public string? LicenseNumber { get; init; }

    /// <summary>Service Provider only.</summary>
    public string? BusinessName { get; init; }

    /// <summary>Service Provider only.</summary>
    public string? Description { get; init; }

    /// <summary>Service Provider only: categories they offer.</summary>
    public IReadOnlyList<Guid> CategoryIds { get; init; } = [];
}
