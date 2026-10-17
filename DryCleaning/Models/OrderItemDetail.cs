namespace DryCleaning.Models;

public sealed class OrderItemDetail
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid ServiceItemId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ServiceItemName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public string Icon { get; init; } = Helpers.AppIcons.BasketShopping;
    public string? Notes { get; init; }
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public int EstimatedDays { get; init; }

    public string Title => $"{ProductName} × {Quantity}";
    public string Subtitle => $"{CategoryName} · {ServiceItemName}";
}
