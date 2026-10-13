using DryCleaning.Models;

namespace DryCleaning.Services;

/// <summary>Basket for one provider, plus the chosen pickup address and slot.</summary>
public interface ICartService
{
    event EventHandler? Changed;

    Guid? ProviderId { get; }
    string ProviderName { get; }
    IReadOnlyList<CartLine> Lines { get; }
    int ItemCount { get; }
    bool IsEmpty { get; }
    decimal SubTotal { get; }
    decimal DeliveryFee { get; }
    decimal Tax { get; }
    decimal Total { get; }
    int EstimatedDays { get; }

    Address? PickupAddress { get; set; }
    DateTime? PickupDate { get; set; }
    string? PickupSlot { get; set; }

    bool BelongsToOtherProvider(Guid providerId);
    void Add(Guid providerId, string providerName, CartLine line);
    void UpdateQuantity(Guid lineId, int quantity);
    void Remove(Guid lineId);
    void Clear();
}
