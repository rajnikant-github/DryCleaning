using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IProviderCatalogService
{
    Task<IReadOnlyList<Category>> GetProviderCategoriesAsync();
    Task<IReadOnlyList<ProductSummary>> GetProductsAsync(Guid categoryId);
    Task<Product> GetProductAsync(Guid productId);
    Task<Product> SaveProductAsync(Product product);
    Task SetProductActiveAsync(Guid productId, bool isActive);
    Task<IReadOnlyList<ServiceItem>> GetServiceItemsAsync(Guid productId);
    Task<ServiceItem> GetServiceItemAsync(Guid serviceItemId);
    Task<ServiceItem> SaveServiceItemAsync(ServiceItem serviceItem);
    Task SetServiceItemActiveAsync(Guid serviceItemId, bool isActive);
}
