using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Provider catalog management (P11). Providers can't create global Categories.</summary>
public sealed class ProviderCatalogService(AppDbContext db, CurrentProfile profile)
{
    public async Task<IReadOnlyList<ProviderCategoryDto>> GetCategoriesAsync(CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        var enabled = await db.ServiceProviderCategories
            .Where(pc => pc.ServiceProviderId == provider.Id && pc.IsActive)
            .Select(pc => pc.CategoryId)
            .ToListAsync(ct);

        return (await db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync(ct))
            .Select(c => new ProviderCategoryDto { CategoryId = c.Id, Name = c.Name, IsEnabled = enabled.Contains(c.Id) })
            .ToList();
    }

    public async Task<IReadOnlyList<ProviderCategoryDto>> SetCategoriesAsync(SetProviderCategoriesRequest request, CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        var valid = await db.Categories.Where(c => c.IsActive && request.CategoryIds.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);
        var links = await db.ServiceProviderCategories.Where(pc => pc.ServiceProviderId == provider.Id).ToListAsync(ct);

        foreach (var link in links)
            link.IsActive = valid.Contains(link.CategoryId);
        foreach (var categoryId in valid.Where(id => links.All(l => l.CategoryId != id)))
            db.ServiceProviderCategories.Add(new ServiceProviderCategory { ServiceProviderId = provider.Id, CategoryId = categoryId });

        await db.SaveChangesAsync(ct);
        return await GetCategoriesAsync(ct);
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(Guid? categoryId, CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        var products = await db.Products
            .Include(p => p.Category)
            .Include(p => p.ServiceItems)
            .Where(p => p.ServiceProviderId == provider.Id && (categoryId == null || p.CategoryId == categoryId))
            .OrderBy(p => p.Category.DisplayOrder).ThenBy(p => p.DisplayOrder)
            .AsSplitQuery()
            .ToListAsync(ct);
        return products.Select(p => p.ToDto()).ToList();
    }

    public async Task<ProductDto> GetProductAsync(Guid productId, CancellationToken ct) => (await LoadProductAsync(productId, ct)).ToDto();

    public async Task<ServiceItemDto> GetServiceItemAsync(Guid serviceItemId, CancellationToken ct) =>
        (await LoadServiceItemAsync(serviceItemId, ct)).ToDto();

    public async Task<ProductDto> CreateProductAsync(ProductRequest request, CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        await EnsureCategoryEnabledAsync(provider.Id, request.CategoryId, ct);

        var product = new Product
        {
            ServiceProviderId = provider.Id,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder
                ?? await db.Products.CountAsync(p => p.ServiceProviderId == provider.Id && p.CategoryId == request.CategoryId, ct) + 1
        };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return (await LoadProductAsync(product.Id, ct)).ToDto();
    }

    public async Task<ProductDto> UpdateProductAsync(Guid productId, ProductRequest request, CancellationToken ct)
    {
        var product = await LoadProductAsync(productId, ct);
        if (product.CategoryId != request.CategoryId)
            await EnsureCategoryEnabledAsync(product.ServiceProviderId, request.CategoryId, ct);

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = request.Description;
        product.ImageUrl = request.ImageUrl;
        product.IsActive = request.IsActive;
        if (request.DisplayOrder is not null)
            product.DisplayOrder = request.DisplayOrder.Value;
        await db.SaveChangesAsync(ct);
        return (await LoadProductAsync(productId, ct)).ToDto();
    }

    public async Task SetProductActiveAsync(Guid productId, bool isActive, CancellationToken ct)
    {
        var product = await LoadProductAsync(productId, ct);
        product.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ServiceItemDto> CreateServiceItemAsync(Guid productId, ServiceItemRequest request, CancellationToken ct)
    {
        var product = await LoadProductAsync(productId, ct);
        var item = new ServiceItem
        {
            ProductId = product.Id,
            Name = request.Name.Trim(),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            Price = Math.Round(request.Price, 2),
            EstimatedDays = request.EstimatedDays,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder ?? product.ServiceItems.Count + 1
        };
        db.ServiceItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item.ToDto();
    }

    public async Task<ServiceItemDto> UpdateServiceItemAsync(Guid serviceItemId, ServiceItemRequest request, CancellationToken ct)
    {
        var item = await LoadServiceItemAsync(serviceItemId, ct);
        item.Name = request.Name.Trim();
        item.Description = request.Description;
        item.ImageUrl = request.ImageUrl;
        item.Price = Math.Round(request.Price, 2);
        item.EstimatedDays = request.EstimatedDays;
        item.IsActive = request.IsActive;
        if (request.DisplayOrder is not null)
            item.DisplayOrder = request.DisplayOrder.Value;
        await db.SaveChangesAsync(ct);
        return item.ToDto();
    }

    public async Task SetServiceItemActiveAsync(Guid serviceItemId, bool isActive, CancellationToken ct)
    {
        var item = await LoadServiceItemAsync(serviceItemId, ct);
        item.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    private async Task<Product> LoadProductAsync(Guid productId, CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        return await db.Products
                   .Include(p => p.Category)
                   .Include(p => p.ServiceItems)
                   .FirstOrDefaultAsync(p => p.Id == productId && p.ServiceProviderId == provider.Id, ct)
               ?? throw new NotFoundException("Product not found.");
    }

    private async Task<ServiceItem> LoadServiceItemAsync(Guid serviceItemId, CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        return await db.ServiceItems
                   .Include(s => s.Product)
                   .FirstOrDefaultAsync(s => s.Id == serviceItemId && s.Product.ServiceProviderId == provider.Id, ct)
               ?? throw new NotFoundException("Service item not found.");
    }

    private async Task EnsureCategoryEnabledAsync(Guid providerId, Guid categoryId, CancellationToken ct)
    {
        var enabled = await db.ServiceProviderCategories.AnyAsync(
            pc => pc.ServiceProviderId == providerId && pc.CategoryId == categoryId && pc.IsActive && pc.Category.IsActive, ct);
        if (!enabled)
            throw new BadRequestException("Enable this category for your business before adding products to it.");
    }
}
