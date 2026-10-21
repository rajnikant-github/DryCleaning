using DryCleaning.Configuration;
using DryCleaning.Models;

namespace DryCleaning.Services;

/// <summary>In-memory cart (singleton). Prices shown are estimates – the server recalculates on order.</summary>
public sealed class CartService(AppSettings settings) : ICartService
{
    private readonly List<CartLine> _lines = [];

    public event EventHandler? Changed;

    public Guid? ProviderId { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public IReadOnlyList<CartLine> Lines => _lines;
    public int ItemCount => _lines.Sum(l => l.Quantity);
    public bool IsEmpty => _lines.Count == 0;
    public decimal SubTotal => _lines.Sum(l => l.LineTotal);
    public int EstimatedDays => _lines.Count == 0 ? 0 : _lines.Max(l => l.EstimatedDays);

    public Address? PickupAddress { get; set; }
    public DateTime? PickupDate { get; set; }
    public string? PickupSlot { get; set; }

    public bool BelongsToOtherProvider(Guid providerId) => ProviderId is not null && ProviderId != providerId && !IsEmpty;

    public void Add(Guid providerId, string providerName, CartLine line)
    {
        if (BelongsToOtherProvider(providerId))
            _lines.Clear();

        ProviderId = providerId;
        ProviderName = providerName;

        var existing = _lines.FirstOrDefault(l => l.ServiceItemId == line.ServiceItemId && l.Notes == line.Notes && l.PhotoPath == line.PhotoPath);
        if (existing is not null)
            existing.Quantity += line.Quantity;
        else
            _lines.Add(line);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateQuantity(Guid lineId, int quantity)
    {
        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
            return;

        if (quantity <= 0)
            _lines.Remove(line);
        else
            line.Quantity = quantity;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(Guid lineId)
    {
        _lines.RemoveAll(l => l.Id == lineId);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _lines.Clear();
        ProviderId = null;
        ProviderName = string.Empty;
        PickupDate = null;
        PickupSlot = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
