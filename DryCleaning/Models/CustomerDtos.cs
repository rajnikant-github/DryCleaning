namespace DryCleaning.Models;

// Read models for the Customer app (shape of future API responses).

public enum CustomerOrderFilter
{
    Active,
    Completed,
    Cancelled
}

public sealed class ProviderListing
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Locality { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
    public decimal? StartingPrice { get; init; }
    public int? FastestDays { get; init; }
}

public sealed class CatalogProduct
{
    public Guid Id { get; init; }
    public Guid ProviderId { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal? StartingPrice { get; init; }
    public IReadOnlyList<ServiceItem> ServiceItems { get; init; } = [];
}

public sealed class PopularServiceItem
{
    public Guid ServiceItemId { get; init; }
    public Guid ProductId { get; init; }
    public Guid ProviderId { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ServiceItemName { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int EstimatedDays { get; init; }
}

public sealed class CustomerHomeData
{
    public string FirstName { get; init; } = string.Empty;
    public Address? Address { get; init; }
    public IReadOnlyList<Category> Categories { get; init; } = [];
    public OrderSummary? ActiveOrder { get; init; }
    public IReadOnlyList<OrderSummary> RecentOrders { get; init; } = [];
    public IReadOnlyList<PopularServiceItem> PopularItems { get; init; } = [];
    public int UnreadNotifications { get; init; }
}

public sealed class CustomerProfile
{
    public required User User { get; init; }
    public Address? Address { get; init; }
    public int OrderCount { get; init; }
}

/// <summary>A Google Places autocomplete suggestion.</summary>
public sealed class PlacePrediction
{
    public string PlaceId { get; init; } = string.Empty;
    public string MainText { get; init; } = string.Empty;
    public string SecondaryText { get; init; } = string.Empty;
}

public sealed record PaymentResult(bool Success, string? Reference, string? Message);

/// <summary>Pickup windows offered on the Pickup Time screen.</summary>
public static class PickupSlots
{
    public static readonly IReadOnlyList<(TimeSpan Start, TimeSpan End)> All =
    [
        (new TimeSpan(9, 0, 0), new TimeSpan(11, 0, 0)),
        (new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0)),
        (new TimeSpan(13, 0, 0), new TimeSpan(15, 0, 0)),
        (new TimeSpan(15, 0, 0), new TimeSpan(17, 0, 0)),
        (new TimeSpan(17, 0, 0), new TimeSpan(19, 0, 0)),
    ];

    public static string Label((TimeSpan Start, TimeSpan End) slot) =>
        $"{DateTime.Today.Add(slot.Start):hh:mm tt} – {DateTime.Today.Add(slot.End):hh:mm tt}";
}
