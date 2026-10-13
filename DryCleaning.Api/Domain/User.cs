namespace DryCleaning.Api.Domain;

public class User : EntityBase
{
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Customer? Customer { get; set; }
    public Provider? ServiceProvider { get; set; }
    public Driver? Driver { get; set; }
}
