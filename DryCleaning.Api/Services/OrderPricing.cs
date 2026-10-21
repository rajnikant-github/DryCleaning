using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Services;

/// <summary>
/// The one place an order's price is worked out (used for real orders, quotes and the admin preview):
/// <list type="number">
///   <item>Items subtotal and the pickup &amp; delivery fee come in.</item>
///   <item>Each extra charge is a fixed amount, or a percentage of the items (or of items + delivery), rounded to the cent.
///         Charges never build on each other.</item>
///   <item>VAT comes last: rate × (items + delivery if the VAT setting says so + the extra charges marked taxable), rounded to the cent.</item>
///   <item>Total = items + delivery + extra charges + VAT.</item>
/// </list>
/// </summary>
public static class OrderPricing
{
    public static PriceBreakdown Calculate(decimal itemsSubTotal, decimal deliveryFee, IEnumerable<ExtraCharge> charges, VatSetting vat)
    {
        var extras = new List<PricedExtraCharge>();
        foreach (var charge in charges.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ThenBy(c => c.CreatedAt))
        {
            if (charge.Type == ExtraChargeType.Percentage)
            {
                var basis = charge.PercentBase == ChargeBase.ItemsAndDelivery ? itemsSubTotal + deliveryFee : itemsSubTotal;
                extras.Add(new PricedExtraCharge
                {
                    Name = charge.Name,
                    Amount = Round(basis * charge.Percent / 100m),
                    Percent = charge.Percent,
                    Basis = $"{charge.Percent:0.##}% of {(charge.PercentBase == ChargeBase.ItemsAndDelivery ? "items and delivery" : "items")}",
                    IsTaxable = charge.IsTaxable,
                    DisplayOrder = charge.DisplayOrder
                });
            }
            else
            {
                extras.Add(new PricedExtraCharge { Name = charge.Name, Amount = Round(charge.Amount), IsTaxable = charge.IsTaxable, DisplayOrder = charge.DisplayOrder });
            }
        }

        var vatBase = itemsSubTotal + (vat.AppliesToDelivery ? deliveryFee : 0m) + extras.Where(e => e.IsTaxable).Sum(e => e.Amount);
        var tax = vat.IsEnabled ? Round(vatBase * vat.Rate / 100m) : 0m;

        return new PriceBreakdown
        {
            SubTotal = itemsSubTotal,
            DeliveryFee = deliveryFee,
            ExtraCharges = extras,
            Tax = tax,
            VatRate = vat.IsEnabled ? vat.Rate : 0m,
            Total = itemsSubTotal + deliveryFee + extras.Sum(e => e.Amount) + tax
        };
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
