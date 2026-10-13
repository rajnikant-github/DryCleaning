using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Cancel / reject / reprocess.</summary>
public sealed class ReasonRequest
{
    public string Reason { get; set; } = string.Empty;
}
