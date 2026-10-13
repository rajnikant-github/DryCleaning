using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>D03 – a driver job with everything the job screens need.</summary>
public sealed class DriverJobDetailDto
{
    public required DriverJobDto Job { get; init; }
    public required DriverJobStep NextStep { get; init; }
    public required bool IsMine { get; init; }
    public required string CustomerName { get; init; }

    /// <summary>Only shared while the job is the caller's own and active.</summary>
    public required string? CustomerPhone { get; init; }

    public required string ProviderName { get; init; }

    /// <summary>Only shared while the job is the caller's own and active.</summary>
    public required string? ProviderPhone { get; init; }

    public required IReadOnlyList<OrderItemDto> Items { get; init; }
}
