using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST/PUT api/provider/products.</summary>
public sealed class ProductRequest
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
