using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>One bar of the earnings chart.</summary>
public sealed class EarningsBarDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public decimal Amount { get; set; }
    public int Jobs { get; set; }
}
