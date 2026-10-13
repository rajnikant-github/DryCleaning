namespace DryCleaning.Models;

public sealed class CatalogProduct
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal? StartingPrice { get; init; }
    public IReadOnlyList<ServiceItem> ServiceItems { get; init; } = [];
}
