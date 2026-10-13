using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ReorderRequest
{
    /// <summary>All category ids in the desired display order.</summary>
    public IReadOnlyList<Guid> CategoryIds { get; set; } = [];
}
