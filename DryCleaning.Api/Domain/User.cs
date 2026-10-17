namespace DryCleaning.Api.Domain;

public class User : EntityBase
{
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Profile photo: a stored path (see IFileStorage), not a URL. Null = initials avatar.</summary>
    public string? PhotoUrl { get; set; }

    public Customer? Customer { get; set; }
    public Provider? ServiceProvider { get; set; }
    public Driver? Driver { get; set; }
}
