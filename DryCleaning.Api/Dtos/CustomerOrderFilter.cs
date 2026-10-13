using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public enum CustomerOrderFilter
{
    Active,
    Completed,
    Cancelled,
    All
}
