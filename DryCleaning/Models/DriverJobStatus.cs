namespace DryCleaning.Models;

public enum DriverJobStatus
{
    /// <summary>Published to the driver marketplace, no driver yet.</summary>
    Pending,
    /// <summary>A driver accepted the job (not collected yet).</summary>
    Assigned,
    /// <summary>The driver has the items and is heading to the drop-off.</summary>
    InProgress,
    Completed,
    Cancelled
}
