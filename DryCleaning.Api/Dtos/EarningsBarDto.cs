using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class EarningsBarDto
{
    public required string Label { get; init; }
    public required DateTime Start { get; init; }
    public required decimal Amount { get; init; }
    public required int Jobs { get; init; }
}
