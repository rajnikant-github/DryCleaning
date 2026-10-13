using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

public sealed class MockProviderCatalogService(MockDataStore store, ISessionService session) : IProviderCatalogService
{
    private Guid ProviderId => session.ProviderId ?? store.DemoProvider.Id;

    public async Task<IReadOnlyList<Category>> GetProviderCategoriesAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var enabled = store.ServiceProviderCategories
                .Where(pc => pc.ServiceProviderId == ProviderId && pc.IsActive)
                .Select(pc => pc.CategoryId)
                .ToHashSet();

            return store.Categories
                .Where(c => c.IsActive && enabled.Contains(c.Id))
                .OrderBy(c => c.DisplayOrder)
                .ToList();
        }
    }

    public async Task<IReadOnlyList<ProductSummary>> GetProductsAsync(Guid categoryId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            return store.Products
                .Where(p => p.ServiceProviderId == ProviderId && p.CategoryId == categoryId)
                .OrderBy(p => p.DisplayOrder)
                .Select(p =>
                {
                    var items = store.ServiceItems.Where(s => s.ProductId == p.Id).ToList();
                    var active = items.Where(s => s.IsActive).ToList();
                    return new ProductSummary
                    {
                        Product = p,
                        ServiceItemCount = items.Count,
                        StartingPrice = active.Count == 0 ? null : active.Min(s => s.Price)
                    };
                })
                .ToList();
        }
    }

    public async Task<Product> GetProductAsync(Guid productId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.Products.FirstOrDefault(p => p.Id == productId) ?? throw new KeyNotFoundException("Product not found.");
    }

    public async Task<Product> SaveProductAsync(Product product)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var existing = store.Products.FirstOrDefault(p => p.Id == product.Id);
            if (existing is null)
            {
                if (product.Id == Guid.Empty)
                    product.Id = Guid.NewGuid();
                product.ServiceProviderId = ProviderId;
                product.DisplayOrder = store.Products.Count(p => p.CategoryId == product.CategoryId && p.ServiceProviderId == ProviderId) + 1;
                product.CreatedAt = product.UpdatedAt = DateTime.Now;
                store.Products.Add(product);
                return product;
            }

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.CategoryId = product.CategoryId;
            existing.IsActive = product.IsActive;
            existing.UpdatedAt = DateTime.Now;
            return existing;
        }
    }

    public async Task SetProductActiveAsync(Guid productId, bool isActive)
    {
        await Task.Delay(150);
        lock (store.Gate)
        {
            var product = store.Products.First(p => p.Id == productId);
            product.IsActive = isActive;
            product.UpdatedAt = DateTime.Now;
        }
    }

    public async Task<IReadOnlyList<ServiceItem>> GetServiceItemsAsync(Guid productId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.ServiceItems.Where(s => s.ProductId == productId).OrderBy(s => s.DisplayOrder).ToList();
    }

    public async Task<ServiceItem> GetServiceItemAsync(Guid serviceItemId)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.ServiceItems.FirstOrDefault(s => s.Id == serviceItemId) ?? throw new KeyNotFoundException("Service item not found.");
    }

    public async Task<ServiceItem> SaveServiceItemAsync(ServiceItem serviceItem)
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
        {
            var existing = store.ServiceItems.FirstOrDefault(s => s.Id == serviceItem.Id);
            if (existing is null)
            {
                if (serviceItem.Id == Guid.Empty)
                    serviceItem.Id = Guid.NewGuid();
                serviceItem.DisplayOrder = store.ServiceItems.Count(s => s.ProductId == serviceItem.ProductId) + 1;
                serviceItem.CreatedAt = serviceItem.UpdatedAt = DateTime.Now;
                store.ServiceItems.Add(serviceItem);
                return serviceItem;
            }

            existing.Name = serviceItem.Name;
            existing.Description = serviceItem.Description;
            existing.Price = serviceItem.Price;
            existing.EstimatedDays = serviceItem.EstimatedDays;
            existing.IsActive = serviceItem.IsActive;
            existing.UpdatedAt = DateTime.Now;
            return existing;
        }
    }

    public async Task SetServiceItemActiveAsync(Guid serviceItemId, bool isActive)
    {
        await Task.Delay(150);
        lock (store.Gate)
        {
            var item = store.ServiceItems.First(s => s.Id == serviceItemId);
            item.IsActive = isActive;
            item.UpdatedAt = DateTime.Now;
        }
    }
}
