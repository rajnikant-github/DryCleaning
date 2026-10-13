using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public static class PagingExtensions
{
    public static async Task<(List<T> Items, int Total)> PageAsync<T>(this IQueryable<T> query, PagedRequest request, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return (items, total);
    }
}
