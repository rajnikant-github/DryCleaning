using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ProviderOrderCountsDto
{
    public required int New { get; init; }
    public required int Incoming { get; init; }
    public required int InProcess { get; init; }
    public required int Ready { get; init; }
    public required int Completed { get; init; }
    public required int Cancelled { get; init; }
    public required int Disputed { get; init; }
}
