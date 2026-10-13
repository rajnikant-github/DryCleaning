using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>A category the provider can enable.</summary>
public sealed class ProviderCategoryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
