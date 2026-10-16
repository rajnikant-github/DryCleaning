using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>A driver job change seen during SaveChanges, including the driver it was taken from (release/reassign).</summary>
public sealed class CapturedJobChange
{
    public required Guid JobId { get; init; }
    public required Guid OrderId { get; init; }
    public required DriverJobType JobType { get; init; }
    public required DriverJobStatus Status { get; init; }
    public required Guid? DriverId { get; init; }
    public required Guid? PreviousDriverId { get; init; }
}
