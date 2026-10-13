namespace DryCleaning.Models;

public sealed class ProductSummary
{
    public required Product Product { get; init; }
    public int ServiceItemCount { get; init; }
    public decimal? StartingPrice { get; init; }
}
