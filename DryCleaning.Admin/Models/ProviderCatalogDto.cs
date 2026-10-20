namespace DryCleaning.Admin.Models;

public sealed class ProviderCatalogDto
{
    public Guid ProviderId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Currency { get; set; } = "ZAR";
    public List<CatalogCategoryDto> Categories { get; set; } = [];
}
