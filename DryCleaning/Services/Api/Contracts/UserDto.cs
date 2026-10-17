using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Signed-in user.</summary>
public sealed class UserDto
{
    public Guid Id { get; set; }
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? ProfileId { get; set; }
    public bool IsActive { get; set; }
    public string? PhotoUrl { get; set; }
}
