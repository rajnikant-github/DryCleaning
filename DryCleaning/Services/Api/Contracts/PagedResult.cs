namespace DryCleaning.Services.Api.Contracts;

/// <summary>One page of a list endpoint.</summary>
public sealed class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
