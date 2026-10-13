using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/catalog/popular.</summary>
public sealed class PopularServiceItemDto
{
    public Guid ServiceItemId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ServiceItemName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public int OrderedQuantity { get; set; }
}
