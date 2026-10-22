using DryCleaning.Api.Infrastructure.Storage;
using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Customer-facing catalog: Category → Provider → Product → Service Item.</summary>
public sealed class PublicCatalogService(AppDbContext db, IOptions<MarketplaceOptions> marketplace, RatingService ratings)
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
        var summaries = await ratings.SummariesAsync(items.Select(p => p.Id).ToList(), ct);
        var dtos = items.Select(p => ToSummary(p, categoryId, summaries.GetValueOrDefault(p.Id))).ToList();

        return new PagedResult<ProviderSummaryDto>
        {
            Items = dtos,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    /// <summary>C06 – one provider's card (provider header on the products screen).</summary>
    public async Task<ProviderSummaryDto> GetProviderAsync(Guid providerId, Guid? categoryId, CancellationToken ct)
    {
        var provider = await db.ServiceProviders
            .Include(p => p.Address)
            .Include(p => p.Categories).ThenInclude(c => c.Category)
            .Include(p => p.Products).ThenInclude(p => p.ServiceItems)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == providerId && p.IsActive && p.User.IsActive, ct)
            ?? throw new NotFoundException("Service provider not found.");
        var summaries = await ratings.SummariesAsync([provider.Id], ct);
        return ToSummary(provider, categoryId, summaries.GetValueOrDefault(provider.Id));
    }

    /// <summary>C08 – one product with its active service items (prices).</summary>
    public async Task<ProductDto> GetProductAsync(Guid productId, CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.ServiceItems)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && p.Category.IsActive &&
                                      p.ServiceProvider.IsActive && p.ServiceProvider.User.IsActive, ct)
            ?? throw new NotFoundException("Product not found.");
        return product.ToDto(activeItemsOnly: true);
    }

    private static ProviderSummaryDto ToSummary(Provider p, Guid? categoryId, (double Average, int Count) rating)
    {
        var activeItems = p.Products
            .Where(pr => pr.IsActive && (categoryId == null || pr.CategoryId == categoryId))
            .SelectMany(pr => pr.ServiceItems.Where(s => s.IsActive))
            .ToList();
        return new ProviderSummaryDto
        {
            Id = p.Id,
            BusinessName = p.BusinessName,
            Description = p.Description,
            LogoUrl = FileUrls.Resolve(p.LogoUrl),
            Locality = p.Address?.SubLocality ?? p.Address?.Locality,
            Categories = p.Categories.Where(c => c.IsActive && c.Category.IsActive).Select(c => c.Category.Name).ToList(),
            StartingPrice = activeItems.Count == 0 ? null : activeItems.Min(s => s.Price),
            FastestDays = activeItems.Count == 0 ? null : activeItems.Min(s => s.EstimatedDays),
            RatingAverage = rating.Count == 0 ? null : rating.Average,
            RatingCount = rating.Count
        };
    }

    /// <summary>
    /// C05 – most ordered service items in the last 30 days (by quantity), active providers/items only.
    /// Falls back to the cheapest active items when there's no recent order data.
    /// </summary>
    public async Task<IReadOnlyList<PopularServiceItemDto>> GetPopularItemsAsync(int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 20);
        var since = DateTime.UtcNow.AddDays(-30);

        var ranked = await db.OrderItems
            .Where(i => i.Order.CreatedAt >= since && i.Order.Status != OrderStatus.Cancelled)
            .GroupBy(i => i.ServiceItemId)
            .Select(g => new { ServiceItemId = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .OrderByDescending(g => g.Quantity)
            .Take(take * 3) // headroom for items that have since been deactivated
            .ToListAsync(ct);

        var ids = ranked.Select(r => r.ServiceItemId).ToList();
        var items = await AvailableItems().Where(s => ids.Contains(s.Id)).ToListAsync(ct);
        var result = ranked
            .Select(r => (Item: items.FirstOrDefault(s => s.Id == r.ServiceItemId), r.Quantity))
            .Where(x => x.Item is not null)
            .Take(take)
            .Select(x => ToPopular(x.Item!, x.Quantity))
            .ToList();

        if (result.Count < take)
        {
            var have = result.Select(r => r.ServiceItemId).ToHashSet();
            var fallback = await AvailableItems()
                .Where(s => !have.Contains(s.Id))
                .OrderBy(s => s.Price)
                .Take(take - result.Count)
                .ToListAsync(ct);
            result.AddRange(fallback.Select(s => ToPopular(s, 0)));
        }

        return result;
    }

    private IQueryable<ServiceItem> AvailableItems() => db.ServiceItems
        .Include(s => s.Product).ThenInclude(p => p.Category)
        .Include(s => s.Product).ThenInclude(p => p.ServiceProvider)
        .Where(s => s.IsActive && s.Product.IsActive && s.Product.Category.IsActive &&
                    s.Product.ServiceProvider.IsActive && s.Product.ServiceProvider.User.IsActive &&
                    s.Product.ServiceProvider.Categories.Any(c => c.CategoryId == s.Product.CategoryId && c.IsActive));

    private static PopularServiceItemDto ToPopular(ServiceItem s, int quantity) => new()
    {
        ServiceItemId = s.Id,
        ProductId = s.ProductId,
        ProviderId = s.Product.ServiceProviderId,
        ProviderName = s.Product.ServiceProvider.BusinessName,
        CategoryId = s.Product.CategoryId,
        CategoryName = s.Product.Category.Name,
        ProductName = s.Product.Name,
        ServiceItemName = s.Name,
        Price = s.Price,
        EstimatedDays = s.EstimatedDays,
        OrderedQuantity = quantity
    };

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
            .Select(c => new CatalogCategoryDto
            {
                Id = c.CategoryId,
                Name = c.Category.Name,
                Description = c.Category.Description,
                ImageUrl = c.Category.ImageUrl,
                Products = provider.Products
                    .Where(p => p.CategoryId == c.CategoryId && p.IsActive && p.ServiceItems.Any(s => s.IsActive))
                    .OrderBy(p => p.DisplayOrder)
                    .Select(p => p.ToDto(activeItemsOnly: true))
                    .ToList()
            })
            .ToList();

        return new ProviderCatalogDto
        {
            ProviderId = provider.Id,
            BusinessName = provider.BusinessName,
            Currency = marketplace.Value.Currency,
            Categories = categories
        };
    }
}
