namespace DryCleaning.Models;

public sealed class DriverAssignment
{
    public Guid DriverJobId { get; init; }
    public DriverJobType JobType { get; init; }
    public DriverJobStatus Status { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public string DriverPhone { get; init; } = string.Empty;
    public string? DriverPhotoUrl { get; init; }
    public string LicenseNumber { get; init; } = string.Empty;
    public DateTime? AssignedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
