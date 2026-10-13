using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class AdminOrdersQuery : Common.PagedRequest
{
    public OrderStatus? Status { get; set; }
}
