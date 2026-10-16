using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>A driver job was published, taken, released, progressed or cancelled.</summary>
public sealed class DriverJobChangedEvent
{
    public required Guid JobId { get; init; }
    public required Guid OrderId { get; init; }
    public required DriverJobType JobType { get; init; }
    public required DriverJobStatus Status { get; init; }
}
