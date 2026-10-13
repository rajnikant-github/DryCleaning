namespace DryCleaning.Models;

/// <summary>Maps to the <c>ServiceProvider</c> table (renamed to avoid clashing with DI's ServiceProvider).</summary>
public sealed class Provider : EntityBase
{
    public Guid UserId { get; set; }
    public Guid AddressId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
