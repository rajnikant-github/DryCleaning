namespace DryCleaning.Admin.Models;

public sealed class CatalogCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ProductDto> Products { get; set; } = [];
}
