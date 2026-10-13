namespace DryCleaning.Models;

public sealed class PopularServiceItem
{
    public Guid ServiceItemId { get; init; }
    public Guid ProductId { get; init; }
    public Guid ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ServiceItemName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int EstimatedDays { get; init; }
}
