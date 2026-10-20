namespace DryCleaning.Admin.Models;

public sealed class UserDto
{
    public Guid Id { get; set; }
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
}
