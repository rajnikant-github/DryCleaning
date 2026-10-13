using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>PUT api/provider/categories.</summary>
public sealed class SetProviderCategoriesRequest
{
    public List<Guid> CategoryIds { get; set; } = [];
}
