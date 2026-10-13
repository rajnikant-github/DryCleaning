using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>C05 – "Popular right now" on the customer home screen.</summary>
public sealed class PopularServiceItemDto
{
    public required Guid ServiceItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required Guid ProviderId { get; init; }
    public required string ProviderName { get; init; }
    public required Guid CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required string ProductName { get; init; }
    public required string ServiceItemName { get; init; }
    public required decimal Price { get; init; }
    public required int EstimatedDays { get; init; }
    public required int OrderedQuantity { get; init; }
}
