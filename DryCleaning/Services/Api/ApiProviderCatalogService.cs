using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>Provider's own catalog: products and service items in the categories they offer.</summary>
public sealed class ApiProviderCatalogService(ApiClient api) : IProviderCatalogService
{
    private static ApiJsonContext Json => ApiJsonContext.Default;

    public async Task<IReadOnlyList<Category>> GetProviderCategoriesAsync()
    {
        var enabledTask = api.GetAsync("api/provider/categories", Json.ListProviderCategoryDto);
        var allTask = api.GetAsync("api/categories", Json.ListCategoryDto, authorize: false);
        await Task.WhenAll(enabledTask, allTask);

        var enabled = enabledTask.Result.Where(c => c.IsEnabled).Select(c => c.CategoryId).ToHashSet();
        return allTask.Result
            .Where(c => c.IsActive && enabled.Contains(c.Id))
            .OrderBy(c => c.DisplayOrder)
            .Select(ApiMapping.ToCategory)
            .ToList();
    }

    public async Task<IReadOnlyList<ProductSummary>> GetProductsAsync(Guid categoryId)
    {
        var products = await api.GetAsync($"api/provider/products?categoryId={categoryId}", Json.ListProductDto);
        return products
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new ProductSummary
            {
                Product = ApiMapping.ToProduct(p),
                ServiceItemCount = p.ServiceItems.Count,
                StartingPrice = p.StartingPrice
            })
            .ToList();
    }

    public async Task<Product> GetProductAsync(Guid productId) =>
        ApiMapping.ToProduct(await api.GetAsync($"api/provider/products/{productId}", Json.ProductDto));

    /// <summary>A product with <see cref="Guid.Empty"/> as its id is created; otherwise it is updated.</summary>
    public async Task<Product> SaveProductAsync(Product product)
    {
        var request = new ProductRequest
        {
            CategoryId = product.CategoryId,
            Name = product.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(product.Description) ? null : product.Description.Trim(),
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive
        };

        var saved = product.Id == Guid.Empty
            ? await api.SendAsync(HttpMethod.Post, "api/provider/products", request, Json.ProductRequest, Json.ProductDto)
            : await api.SendAsync(HttpMethod.Put, $"api/provider/products/{product.Id}", request, Json.ProductRequest, Json.ProductDto);
        return ApiMapping.ToProduct(saved);
    }

    public Task SetProductActiveAsync(Guid productId, bool isActive) =>
        api.SendAsync(HttpMethod.Put, $"api/provider/products/{productId}/active", new SetActiveRequest { IsActive = isActive }, Json.SetActiveRequest);

    public async Task<IReadOnlyList<ServiceItem>> GetServiceItemsAsync(Guid productId)
    {
        var product = await api.GetAsync($"api/provider/products/{productId}", Json.ProductDto);
        return product.ServiceItems.OrderBy(s => s.DisplayOrder).Select(ApiMapping.ToServiceItem).ToList();
    }

    public async Task<ServiceItem> GetServiceItemAsync(Guid serviceItemId) =>
        ApiMapping.ToServiceItem(await api.GetAsync($"api/provider/service-items/{serviceItemId}", Json.ServiceItemDto));

    /// <summary>A service item with <see cref="Guid.Empty"/> as its id is created under its product; otherwise it is updated.</summary>
    public async Task<ServiceItem> SaveServiceItemAsync(ServiceItem serviceItem)
    {
        var request = new ServiceItemRequest
        {
            Name = serviceItem.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(serviceItem.Description) ? null : serviceItem.Description.Trim(),
            ImageUrl = serviceItem.ImageUrl,
            Price = serviceItem.Price,
            EstimatedDays = serviceItem.EstimatedDays,
            IsActive = serviceItem.IsActive
        };

        var saved = serviceItem.Id == Guid.Empty
            ? await api.SendAsync(HttpMethod.Post, $"api/provider/products/{serviceItem.ProductId}/service-items", request, Json.ServiceItemRequest, Json.ServiceItemDto)
            : await api.SendAsync(HttpMethod.Put, $"api/provider/service-items/{serviceItem.Id}", request, Json.ServiceItemRequest, Json.ServiceItemDto);
        return ApiMapping.ToServiceItem(saved);
    }

    public Task SetServiceItemActiveAsync(Guid serviceItemId, bool isActive) =>
        api.SendAsync(HttpMethod.Put, $"api/provider/service-items/{serviceItemId}/active", new SetActiveRequest { IsActive = isActive }, Json.SetActiveRequest);
}
