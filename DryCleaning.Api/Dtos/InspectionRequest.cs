using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class InspectionRequest
{
    public IReadOnlyList<InspectionItemRequest> Items { get; set; } = [];
}
