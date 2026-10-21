using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Admin-controlled pickup &amp; delivery pricing, and the fee for one order.</summary>
public sealed class DeliveryPricingService(AppDbContext db, IOptions<MarketplaceOptions> marketplace, IRouteDistanceService routes)
{
    /// <summary>Straight-line distance understates the road distance; this keeps quotes realistic.</summary>
    private const decimal RoadFactor = 1.3m;
    private const double EarthRadiusKm = 6371.0;

    public async Task<DeliveryPricingDto> GetAsync(CancellationToken ct) => ToDto(await RuleAsync(ct));

    public async Task<DeliveryPricingDto> UpdateAsync(DeliveryPricingDto request, CancellationToken ct)
    {
        var rule = await RuleAsync(ct);
        rule.BaseFee = request.BaseFee;
        rule.PricePerKm = request.PricePerKm;
        rule.IncludedKm = request.IncludedKm;
        rule.MinFee = request.MinFee;
        rule.MaxFee = request.MaxFee;
        rule.Touch();
        await db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    /// <summary>
    /// Fee for a pickup address and a provider. Uses the real driving distance from Google Routes; when that can't be measured
    /// it falls back to a straight-line estimate, and without locations on both addresses the base fee applies.
    /// </summary>
    public async Task<DeliveryQuote> QuoteAsync(Address? pickup, Address? provider, CancellationToken ct)
    {
        var rule = await RuleAsync(ct);
        if (pickup is null || provider is null || !HasLocation(pickup) || !HasLocation(provider))
            return new DeliveryQuote { Fee = Clamp(rule, rule.BaseFee) };

        var route = await routes.GetAsync(pickup, provider, ct);
        var km = route?.DistanceKm ?? Math.Round(StraightLineKm(pickup, provider) * RoadFactor, 1);
        var extra = Math.Max(0, km - rule.IncludedKm);
        return new DeliveryQuote
        {
            Fee = Clamp(rule, rule.BaseFee + extra * rule.PricePerKm),
            DistanceKm = km,
            DurationMinutes = route?.DurationMinutes,
            IsEstimate = route is null
        };
    }

    private static decimal Clamp(DeliveryPricing rule, decimal fee)
    {
        fee = Math.Max(fee, rule.MinFee);
        if (rule.MaxFee is { } max)
            fee = Math.Min(fee, max);
        return Math.Round(fee, 2, MidpointRounding.AwayFromZero);
    }

    private static bool HasLocation(Address a) => a.Latitude != 0 || a.Longitude != 0;

    private static decimal StraightLineKm(Address a, Address b)
    {
        static double Rad(decimal degrees) => (double)degrees * Math.PI / 180;
        var dLat = Rad(b.Latitude - a.Latitude);
        var dLon = Rad(b.Longitude - a.Longitude);
        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(a.Latitude)) * Math.Cos(Rad(b.Latitude)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return (decimal)(2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(h))));
    }

    /// <summary>
    /// The single rule row. Pickup &amp; delivery is always charged per kilometre; the admin portal edits these values.
    /// The first time the rule is needed it is created with the starting values below.
    /// </summary>
    private async Task<DeliveryPricing> RuleAsync(CancellationToken ct)
    {
        var rule = await db.DeliveryPricing.FirstOrDefaultAsync(ct);
        if (rule is null)
        {
            rule = StartingRule();
            db.DeliveryPricing.Add(rule);
            await db.SaveChangesAsync(ct);
        }
        else if (rule.PricePerKm <= 0)
        {
            // A rule from the earlier flat-fee days (the admin can no longer save a zero price per km): move it to per-km pricing.
            var starting = StartingRule();
            rule.BaseFee = starting.BaseFee;
            rule.IncludedKm = starting.IncludedKm;
            rule.PricePerKm = starting.PricePerKm;
            rule.MinFee = starting.MinFee;
            rule.MaxFee = starting.MaxFee;
            rule.Touch();
            await db.SaveChangesAsync(ct);
        }

        return rule;
    }

    /// <summary>Starting values: R30 covers the first 2 km, then R5 per km, never below R30 or above R250. Change them in the admin portal.</summary>
    private static DeliveryPricing StartingRule() => new() { BaseFee = 30m, IncludedKm = 2m, PricePerKm = 5m, MinFee = 30m, MaxFee = 250m };

    private DeliveryPricingDto ToDto(DeliveryPricing r) => new()
    {
        BaseFee = r.BaseFee,
        PricePerKm = r.PricePerKm,
        IncludedKm = r.IncludedKm,
        MinFee = r.MinFee,
        MaxFee = r.MaxFee,
        Currency = marketplace.Value.Currency
    };
}
