using DryCleaning.Configuration;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

public sealed class MockCustomerCatalogService(MockDataStore store) : ICustomerCatalogService
{
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList();
    }

    public async Task<Category> GetCategoryAsync(Guid categoryId)
    {
        await Task.Delay(100);
        lock (store.Gate)
            return store.Categories.FirstOrDefault(c => c.Id == categoryId) ?? throw new KeyNotFoundException("Category not found.");
    }

    public async Task<IReadOnlyList<ProviderListing>> GetProvidersAsync(Guid categoryId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            return store.Providers
                .Where(p => p.IsActive && store.ServiceProviderCategories.Any(pc => pc.ServiceProviderId == p.Id && pc.CategoryId == categoryId && pc.IsActive))
                .Select(p => ToListing(p, categoryId))
                .Where(l => l.StartingPrice is not null)
                .OrderBy(l => l.StartingPrice)
                .ToList();
        }
    }

    public async Task<ProviderListing> GetProviderAsync(Guid providerId)
    {
        await Task.Delay(100);
        lock (store.Gate)
            return ToListing(store.ProviderById(providerId), null);
    }

    public async Task<IReadOnlyList<CatalogProduct>> GetProductsAsync(Guid providerId, Guid categoryId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            return store.Products
                .Where(p => p.ServiceProviderId == providerId && p.CategoryId == categoryId && p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .Select(ToCatalogProduct)
                .Where(p => p.ServiceItems.Count > 0)
                .ToList();
        }
    }

    public async Task<CatalogProduct> GetProductAsync(Guid productId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return ToCatalogProduct(store.Products.FirstOrDefault(p => p.Id == productId) ?? throw new KeyNotFoundException("Product not found."));
    }

    public async Task<IReadOnlyList<PopularServiceItem>> GetPopularItemsAsync(int take)
    {
        await Task.Delay(100);
        lock (store.Gate)
        {
            return store.OrderItems
                .GroupBy(i => i.ServiceItemId)
                .OrderByDescending(g => g.Sum(i => i.Quantity))
                .Select(g => store.ServiceItems.First(s => s.Id == g.Key))
                .Where(s => s.IsActive)
                .Take(take)
                .Select(s =>
                {
                    var product = store.Products.First(p => p.Id == s.ProductId);
                    var provider = store.ProviderById(product.ServiceProviderId);
                    return new PopularServiceItem
                    {
                        ServiceItemId = s.Id,
                        ProductId = product.Id,
                        ProviderId = provider.Id,
                        ProviderName = provider.BusinessName,
                        ProductName = product.Name,
                        ServiceItemName = s.Name,
                        Price = s.Price,
                        EstimatedDays = s.EstimatedDays
                    };
                })
                .ToList();
        }
    }

    private ProviderListing ToListing(Provider provider, Guid? categoryId)
    {
        var items = store.Products
            .Where(p => p.ServiceProviderId == provider.Id && p.IsActive && (categoryId == null || p.CategoryId == categoryId))
            .SelectMany(p => store.ServiceItems.Where(s => s.ProductId == p.Id && s.IsActive))
            .ToList();
        var categoryIds = store.ServiceProviderCategories.Where(pc => pc.ServiceProviderId == provider.Id && pc.IsActive).Select(pc => pc.CategoryId).ToList();

        return new ProviderListing
        {
            Id = provider.Id,
            BusinessName = provider.BusinessName,
            Description = provider.Description,
            Locality = store.AddressById(provider.AddressId).SubLocality,
            Categories = store.Categories.Where(c => categoryIds.Contains(c.Id)).OrderBy(c => c.DisplayOrder).Select(c => c.Name).ToList(),
            StartingPrice = items.Count == 0 ? null : items.Min(s => s.Price),
            FastestDays = items.Count == 0 ? null : items.Min(s => s.EstimatedDays)
        };
    }

    private CatalogProduct ToCatalogProduct(Product product)
    {
        var items = store.ServiceItems.Where(s => s.ProductId == product.Id && s.IsActive).OrderBy(s => s.DisplayOrder).ToList();
        return new CatalogProduct
        {
            Id = product.Id,
            ProviderId = product.ServiceProviderId,
            CategoryId = product.CategoryId,
            CategoryName = store.Categories.First(c => c.Id == product.CategoryId).Name,
            Name = product.Name,
            Description = product.Description,
            StartingPrice = items.Count == 0 ? null : items.Min(s => s.Price),
            ServiceItems = items
        };
    }

    public async Task<IReadOnlyList<WorkingHour>> GetWorkingHoursAsync(Guid providerId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.WorkingHours.TryGetValue(providerId, out var hours) ? hours.ToList() : WorkingHour.DefaultWeek();
    }
}
