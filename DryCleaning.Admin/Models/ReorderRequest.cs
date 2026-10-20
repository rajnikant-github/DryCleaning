namespace DryCleaning.Admin.Models;

public sealed class ReorderRequest
{
    public List<Guid> CategoryIds { get; set; } = [];
}
