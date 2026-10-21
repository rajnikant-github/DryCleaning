namespace DryCleaning.Admin.Models;

public sealed class VerificationQueueItemDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public UserRole Role { get; set; }
    public string? Detail { get; set; }
    public ProfileStatus ProfileStatus { get; set; }
    public int PendingCount { get; set; }
    public int VerifiedCount { get; set; }
    public int RejectedCount { get; set; }
    public int RequiredRemaining { get; set; }
    public DateTime? LastSubmittedAt { get; set; }
}
