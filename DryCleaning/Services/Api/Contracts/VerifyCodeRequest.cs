using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Handover / delivery confirmation.</summary>
public sealed class VerifyCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public int? ItemCount { get; set; }
    public string? Notes { get; set; }
}
