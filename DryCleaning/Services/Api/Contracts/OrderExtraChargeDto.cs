namespace DryCleaning.Services.Api.Contracts;

public sealed class OrderExtraChargeDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
