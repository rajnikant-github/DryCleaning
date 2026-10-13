using DryCleaning.Models;

namespace DryCleaning.Services.Api;

/// <summary>Customer catalog: Category → Provider → Product → Service Item (public endpoints).</summary>
public sealed class ApiCustomerCatalogService(ApiClient api) : ICustomerCatalogService
{
    private static ApiJsonContext Json => ApiJsonContext.Default;

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        var categories = await api.GetAsync("api/categories", Json.ListCategoryDto, authorize: false);
        return categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).Select(ApiMapping.ToCategory).ToList();
    }

    public async Task<Category> GetCategoryAsync(Guid categoryId) =>
        ApiMapping.ToCategory(await api.GetAsync($"api/categories/{categoryId}", Json.CategoryDto, authorize: false));

    public async Task<IReadOnlyList<ProviderListing>> GetProvidersAsync(Guid categoryId)
    {
        var page = await api.GetAsync($"api/providers?categoryId={categoryId}&pageSize=100", Json.PagedResultProviderSummaryDto, authorize: false);
        return page.Items
            .Where(p => p.StartingPrice is not null)
            .OrderBy(p => p.StartingPrice)
            .Select(ApiMapping.ToListing)
            .ToList();
    }

    public async Task<ProviderListing> GetProviderAsync(Guid providerId) =>
        ApiMapping.ToListing(await api.GetAsync($"api/providers/{providerId}", Json.ProviderSummaryDto, authorize: false));

    public async Task<IReadOnlyList<CatalogProduct>> GetProductsAsync(Guid providerId, Guid categoryId)
    {
        var catalog = await api.GetAsync($"api/providers/{providerId}/catalog", Json.ProviderCatalogDto, authorize: false);
        return catalog.Categories
            .Where(c => c.Id == categoryId)
            .SelectMany(c => c.Products)
            .OrderBy(p => p.DisplayOrder)
            .Select(ApiMapping.ToCatalogProduct)
            .Where(p => p.ServiceItems.Count > 0)
            .ToList();
    }

    public async Task<CatalogProduct> GetProductAsync(Guid productId) =>
        ApiMapping.ToCatalogProduct(await api.GetAsync($"api/catalog/products/{productId}", Json.ProductDto, authorize: false));

    public async Task<IReadOnlyList<PopularServiceItem>> GetPopularItemsAsync(int take)
    {
        var items = await api.GetAsync($"api/catalog/popular?take={take}", Json.ListPopularServiceItemDto, authorize: false);
        return items.Select(ApiMapping.ToPopular).ToList();
    }
}
