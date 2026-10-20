namespace DryCleaning.Admin.Models;

public sealed class AdminDriverDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int ActiveJobs { get; set; }
    public int CompletedJobs { get; set; }
    public bool IsActive { get; set; }
}
