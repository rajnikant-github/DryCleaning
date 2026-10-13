using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

// Admin

public sealed class DailyCountDto
{
    public required DateOnly Date { get; init; }
    public required int Orders { get; init; }
    public required decimal Revenue { get; init; }
}
