using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST/PUT provider service items.</summary>
public sealed class ServiceItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
