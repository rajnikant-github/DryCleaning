using DryCleaning.Models;

namespace DryCleaning.Services;

/// <summary>Holds the D05 item check between the Verify Items and Confirm Pickup screens.</summary>
public sealed class DriverPickupDraft
{
    public Guid JobId { get; set; }
    public IReadOnlyList<DriverItemCheck> Items { get; set; } = [];
    public string? Notes { get; set; }

    public void Reset(Guid jobId)
    {
        JobId = jobId;
        Items = [];
        Notes = null;
    }
}
