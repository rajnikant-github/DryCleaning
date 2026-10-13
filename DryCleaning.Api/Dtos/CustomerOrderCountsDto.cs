using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class CustomerOrderCountsDto
{
    public required int Active { get; init; }
    public required int Completed { get; init; }
    public required int Cancelled { get; init; }
}
