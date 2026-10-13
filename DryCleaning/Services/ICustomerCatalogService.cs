using DryCleaning.Models;

namespace DryCleaning.Services;

public interface ICustomerCatalogService
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync();
    Task<Category> GetCategoryAsync(Guid categoryId);
    Task<IReadOnlyList<ProviderListing>> GetProvidersAsync(Guid categoryId);
    Task<ProviderListing> GetProviderAsync(Guid providerId);
    Task<IReadOnlyList<CatalogProduct>> GetProductsAsync(Guid providerId, Guid categoryId);
    Task<CatalogProduct> GetProductAsync(Guid productId);
    Task<IReadOnlyList<PopularServiceItem>> GetPopularItemsAsync(int take);
}
