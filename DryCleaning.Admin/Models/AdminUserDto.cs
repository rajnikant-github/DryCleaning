namespace DryCleaning.Admin.Models;

public sealed class AdminUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
}
