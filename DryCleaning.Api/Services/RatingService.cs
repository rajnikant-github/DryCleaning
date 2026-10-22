using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Services;

/// <summary>Customers rate the service provider once per completed order.</summary>
public sealed class RatingService(AppDbContext db, CurrentProfile profile)
{
    public async Task<RatingDto> SubmitAsync(Guid orderId, SubmitRatingRequest request, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        var order = await db.Orders
                        .Include(o => o.Customer).ThenInclude(c => c.User)
                        .Include(o => o.ServiceProvider)
                        .Include(o => o.Rating)
                        .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customer.Id, ct)
                    ?? throw new NotFoundException("Order not found.");

        if (order.Status != OrderStatus.Completed)
            throw new ConflictException("You can rate the provider once the order is completed.");
        if (order.Rating is not null)
            throw new ConflictException("You have already rated this order.");

        var rating = new Rating
        {
            OrderId = order.Id,
            CustomerId = customer.Id,
            ServiceProviderId = order.ServiceProviderId,
            Stars = request.Stars,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim()
        };
        db.Ratings.Add(rating);

        db.Notify(order.ServiceProvider.UserId, order, "New rating",
            $"{Mapping.FirstName(order.Customer.User.Name)} rated {order.OrderNumber} {request.Stars} out of 5.");
        await db.SaveChangesAsync(ct);
        return rating.ToDto(order.OrderNumber, order.Customer.User.Name);
    }

    /// <summary>The signed-in provider's own ratings, newest first. A hidden comment shows as a rating without a comment.</summary>
    public async Task<PagedResult<RatingDto>> GetMineAsync(PagedRequest request, bool onlyWithComment, CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        var query = db.Ratings.Where(r => r.ServiceProviderId == provider.Id);
        if (onlyWithComment)
            query = query.Where(r => r.Comment != null && !r.CommentHidden);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(r => new { Rating = r, r.Order.OrderNumber, CustomerName = r.Customer.User.Name })
            .ToListAsync(ct);

        return new PagedResult<RatingDto>
        {
            Items = rows.Select(x => x.Rating.ToDto(x.OrderNumber, x.CustomerName)).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    /// <summary>Admin list, newest first. Optionally only ratings with a comment, or one provider.</summary>
    public async Task<PagedResult<AdminRatingDto>> GetAdminListAsync(PagedRequest request, bool onlyWithComment, CancellationToken ct)
    {
        var query = db.Ratings.AsQueryable();
        if (onlyWithComment)
            query = query.Where(r => r.Comment != null);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(r => r.Order.OrderNumber.Contains(request.Search) || r.ServiceProvider.BusinessName.Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(r => new AdminRatingDto
            {
                Id = r.Id,
                OrderId = r.OrderId,
                OrderNumber = r.Order.OrderNumber,
                ProviderName = r.ServiceProvider.BusinessName,
                CustomerName = r.Customer.User.Name,
                Stars = r.Stars,
                Comment = r.Comment,
                CommentHidden = r.CommentHidden,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<AdminRatingDto> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = total };
    }

    public async Task SetCommentHiddenAsync(Guid ratingId, bool hidden, CancellationToken ct)
    {
        var rating = await db.Ratings.FirstOrDefaultAsync(r => r.Id == ratingId, ct)
                     ?? throw new NotFoundException("Rating not found.");
        rating.CommentHidden = hidden;
        rating.Touch();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Average stars and count per provider, for the given providers.</summary>
    public async Task<Dictionary<Guid, (double Average, int Count)>> SummariesAsync(IReadOnlyCollection<Guid> providerIds, CancellationToken ct)
    {
        var rows = await db.Ratings
            .Where(r => providerIds.Contains(r.ServiceProviderId))
            .GroupBy(r => r.ServiceProviderId)
            .Select(g => new { g.Key, Average = g.Average(r => (double)r.Stars), Count = g.Count() })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.Key, r => (Math.Round(r.Average, 1), r.Count));
    }

    /// <summary>The newest ratings that have a visible comment.</summary>
    public async Task<List<RatingDto>> LatestWithCommentsAsync(Guid providerId, int take, CancellationToken ct)
    {
        var rows = await db.Ratings
            .Where(r => r.ServiceProviderId == providerId && r.Comment != null && !r.CommentHidden)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .Select(r => new { Rating = r, r.Order.OrderNumber, CustomerName = r.Customer.User.Name })
            .ToListAsync(ct);
        return rows.Select(x => x.Rating.ToDto(x.OrderNumber, x.CustomerName)).ToList();
    }
}
