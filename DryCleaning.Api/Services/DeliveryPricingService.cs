using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Admin-controlled pickup &amp; delivery pricing, and the fee for one order.</summary>
public sealed class DeliveryPricingService(AppDbContext db, IOptions<MarketplaceOptions> marketplace)
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

    /// <summary>Fee and (road) distance for a pickup address and a provider. Without both locations the base fee applies.</summary>
    public async Task<(decimal Fee, decimal? DistanceKm)> QuoteAsync(Address? pickup, Address? provider, CancellationToken ct)
    {
        var rule = await RuleAsync(ct);
        if (pickup is null || provider is null || !HasLocation(pickup) || !HasLocation(provider))
            return (Clamp(rule, rule.BaseFee), null);

        var km = Math.Round(StraightLineKm(pickup, provider) * RoadFactor, 1);
        var extra = Math.Max(0, km - rule.IncludedKm);
        var fee = Clamp(rule, rule.BaseFee + extra * rule.PricePerKm);
        return (fee, km);
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

    /// <summary>The single rule row; created from the old flat fee in appsettings the first time it is needed.</summary>
    private async Task<DeliveryPricing> RuleAsync(CancellationToken ct)
    {
        var rule = await db.DeliveryPricing.FirstOrDefaultAsync(ct);
        if (rule is not null)
            return rule;

        rule = new DeliveryPricing { BaseFee = marketplace.Value.DeliveryFee, MinFee = marketplace.Value.DeliveryFee };
        db.DeliveryPricing.Add(rule);
        await db.SaveChangesAsync(ct);
        return rule;
    }

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
