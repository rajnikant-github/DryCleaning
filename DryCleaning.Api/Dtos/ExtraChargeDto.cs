namespace DryCleaning.Api.Dtos;

public sealed class ExtraChargeDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required decimal Amount { get; init; }
    public required bool IsActive { get; init; }
    public required int DisplayOrder { get; init; }
}
