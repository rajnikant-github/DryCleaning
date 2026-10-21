namespace DryCleaning.Api.Dtos;

public sealed class ExtraChargeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>A fixed amount in the marketplace currency (not a percentage).</summary>
    public decimal Amount { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
