namespace DryCleaning.Models;

/// <summary>Editable business details of the signed-in Service Provider.</summary>
public sealed class ProviderProfileUpdate
{
    public string BusinessName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }

    /// <summary>Current logo URL (kept as is; a new logo is uploaded separately).</summary>
    public string? LogoUrl { get; init; }
}
