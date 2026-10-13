using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public class PagedRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page { get => _page; set => _page = Math.Max(1, value); }
    public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, 100); }

    /// <summary>Optional free-text search.</summary>
    public string? Search { get; set; }
}
