using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class CustomerOrdersQuery : Common.PagedRequest
{
    public CustomerOrderFilter Filter { get; set; } = CustomerOrderFilter.All;
}
