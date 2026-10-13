using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class DriverJobCountsDto
{
    public required int Available { get; init; }
    public required int Mine { get; init; }
    public required int Completed { get; init; }
}
