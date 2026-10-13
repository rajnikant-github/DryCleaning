using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Customer or provider on an order.</summary>
public sealed class PartyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
