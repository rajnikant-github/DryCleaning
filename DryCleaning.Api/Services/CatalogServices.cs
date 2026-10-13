using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>One address per role (V1). Uses Google Places fields.</summary>
public sealed class AddressService(AppDbContext db, ICurrentUser currentUser, CurrentProfile profile)
{
    public async Task<AddressDto?> GetMineAsync(CancellationToken ct)
    {
        var address = await LoadAddressAsync(ct);
        return address?.ToDto();
    }

    public async Task<AddressDto> UpsertMineAsync(UpsertAddressRequest request, CancellationToken ct)
    {
        switch (currentUser.Role)
        {
            case UserRole.Customer:
            {
                var customer = await profile.CustomerAsync(ct);
                customer.Address = Upsert(customer.Address, request);
                break;
            }
            case UserRole.Provider:
            {
                var provider = await profile.ProviderAsync(ct);
                provider.Address = Upsert(provider.Address, request);
                break;
            }
            case UserRole.Driver:
            {
                var driver = await profile.DriverAsync(ct);
                driver.Address = Upsert(driver.Address, request);
                break;
            }
            default:
                throw new ForbiddenException("Admins don't have an address.");
        }

        await db.SaveChangesAsync(ct);
        return (await LoadAddressAsync(ct))!.ToDto();
    }

    private Address Upsert(Address? existing, UpsertAddressRequest request)
    {
        // Addresses referenced by past orders/jobs are kept unchanged – a new row is created instead.
        var address = existing is not null && !IsReferenced(existing.Id) ? existing : new Address();
        address.Apply(request);
        if (address != existing)
            db.Addresses.Add(address);
        return address;
    }

    private bool IsReferenced(Guid addressId) =>
        db.Orders.Any(o => o.PickupAddressId == addressId) ||
        db.DriverJobs.Any(j => j.PickupAddressId == addressId || j.DeliveryAddressId == addressId);

    private async Task<Address?> LoadAddressAsync(CancellationToken ct) => currentUser.Role switch
    {
        UserRole.Customer => (await profile.CustomerAsync(ct)).Address,
        UserRole.Provider => (await profile.ProviderAsync(ct)).Address,
        UserRole.Driver => (await profile.DriverAsync(ct)).Address,
        _ => null
    };
}

/// <summary>Global categories – managed by admins only.</summary>
public sealed class CategoryService(AppDbContext db)
{
    public async Task<IReadOnlyList<CategoryDto>> GetAsync(bool includeInactive, CancellationToken ct) =>
        (await db.Categories
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync(ct))
        .Select(c => c.ToDto()).ToList();

    public async Task<CategoryDto> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await db.Categories.FindAsync([id], ct) ?? throw new NotFoundException("Category not found.")).ToDto();

    public async Task<CategoryDto> CreateAsync(CategoryRequest request, CancellationToken ct)
    {
        await EnsureUniqueNameAsync(request.Name, null, ct);
        var category = new Category();
        Apply(category, request);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, CategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct) ?? throw new NotFoundException("Category not found.");
        await EnsureUniqueNameAsync(request.Name, id, ct);
        Apply(category, request);
        await db.SaveChangesAsync(ct);
        return category.ToDto();
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([id], ct) ?? throw new NotFoundException("Category not found.");
        category.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderAsync(ReorderRequest request, CancellationToken ct)
    {
        var categories = await db.Categories.Where(c => request.CategoryIds.Contains(c.Id)).ToListAsync(ct);
        for (var i = 0; i < request.CategoryIds.Count; i++)
        {
            var category = categories.FirstOrDefault(c => c.Id == request.CategoryIds[i]);
            if (category is not null)
                category.DisplayOrder = i + 1;
        }

        await db.SaveChangesAsync(ct);
    }

    private static void Apply(Category category, CategoryRequest request)
    {
        category.Name = request.Name.Trim();
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;
    }

    private async Task EnsureUniqueNameAsync(string name, Guid? exceptId, CancellationToken ct)
    {
        var trimmed = name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == trimmed && c.Id != exceptId, ct))
            throw new ConflictException($"A category named '{trimmed}' already exists.");
    }
}

/// <summary>Customer-facing catalog: Category → Provider → Product → Service Item.</summary>
public sealed class PublicCatalogService(AppDbContext db, IOptions<MarketplaceOptions> marketplace)
{
    public async Task<PagedResult<ProviderSummaryDto>> GetProvidersAsync(Guid? categoryId, PagedRequest request, CancellationToken ct)
    {
        var query = db.ServiceProviders
            .Include(p => p.Address)
            .Include(p => p.Categories).ThenInclude(c => c.Category)
            .Include(p => p.Products).ThenInclude(p => p.ServiceItems)
            .Where(p => p.IsActive && p.User.IsActive);

        if (categoryId is not null)
            query = query.Where(p => p.Categories.Any(c => c.CategoryId == categoryId && c.IsActive));
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p => p.BusinessName.Contains(request.Search));

        var (items, total) = await query.OrderBy(p => p.BusinessName).AsSplitQuery().PageAsync(request, ct);
        var dtos = items.Select(p =>
        {
            var prices = p.Products
                .Where(pr => pr.IsActive && (categoryId == null || pr.CategoryId == categoryId))
                .SelectMany(pr => pr.ServiceItems.Where(s => s.IsActive))
                .Select(s => s.Price)
                .ToList();
            return new ProviderSummaryDto(
                p.Id, p.BusinessName, p.Description, p.LogoUrl, p.Address?.SubLocality ?? p.Address?.Locality,
                p.Categories.Where(c => c.IsActive && c.Category.IsActive).Select(c => c.Category.Name).ToList(),
                prices.Count == 0 ? null : prices.Min());
        }).ToList();

        return new PagedResult<ProviderSummaryDto>(dtos, request.Page, request.PageSize, total);
    }

    public async Task<ProviderCatalogDto> GetProviderCatalogAsync(Guid providerId, CancellationToken ct)
    {
        var provider = await db.ServiceProviders
            .Include(p => p.Categories).ThenInclude(c => c.Category)
            .Include(p => p.Products).ThenInclude(p => p.ServiceItems)
            .Include(p => p.Products).ThenInclude(p => p.Category)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == providerId && p.IsActive, ct)
            ?? throw new NotFoundException("Service provider not found.");

        var categories = provider.Categories
            .Where(c => c.IsActive && c.Category.IsActive)
            .OrderBy(c => c.Category.DisplayOrder)
            .Select(c => new CatalogCategoryDto(
                c.CategoryId, c.Category.Name, c.Category.Description, c.Category.ImageUrl,
                provider.Products
                    .Where(p => p.CategoryId == c.CategoryId && p.IsActive && p.ServiceItems.Any(s => s.IsActive))
                    .OrderBy(p => p.DisplayOrder)
                    .Select(p => p.ToDto(activeItemsOnly: true))
                    .ToList()))
            .ToList();

        return new ProviderCatalogDto(provider.Id, provider.BusinessName, marketplace.Value.Currency, categories);
    }
}

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
            .Select(c => new ProviderCategoryDto(c.Id, c.Name, enabled.Contains(c.Id)))
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
