namespace DryCleaning.Api.Domain;

public enum DriverJobStatus
{
    /// <summary>Published to the marketplace, no driver yet.</summary>
    Pending,
    /// <summary>A driver accepted the job.</summary>
    Assigned,
    /// <summary>Driver has the items (on the way to destination).</summary>
    InProgress,
    Completed,
    Cancelled
}
