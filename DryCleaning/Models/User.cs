namespace DryCleaning.Models;

public sealed class User : EntityBase
{
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Profile photo: a web address (API) or a file path on the phone (demo data). Null = initials.</summary>
    public string? PhotoUrl { get; set; }
}
