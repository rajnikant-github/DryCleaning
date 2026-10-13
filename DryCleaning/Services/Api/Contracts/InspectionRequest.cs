using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/provider/orders/{id}/inspection.</summary>
public sealed class InspectionRequest
{
    public List<InspectionItemRequest> Items { get; set; } = [];
}
