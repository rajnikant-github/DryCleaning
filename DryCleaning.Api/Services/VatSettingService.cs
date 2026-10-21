using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Services;

/// <summary>The marketplace's VAT rule (one row). Created with 15% on the first read.</summary>
public sealed class VatSettingService(AppDbContext db)
{
    public async Task<VatSetting> CurrentAsync(CancellationToken ct)
    {
        var vat = await db.VatSettings.OrderBy(v => v.CreatedAt).FirstOrDefaultAsync(ct);
        if (vat is not null) return vat;

        vat = new VatSetting();
        db.VatSettings.Add(vat);
        await db.SaveChangesAsync(ct);
        return vat;
    }

    public async Task<VatSettingDto> GetAsync(CancellationToken ct) => ToDto(await CurrentAsync(ct));

    /// <summary>Applies to new orders only; existing orders keep the rate they were charged.</summary>
    public async Task<VatSettingDto> UpdateAsync(VatSettingDto request, CancellationToken ct)
    {
        var vat = await CurrentAsync(ct);
        vat.Rate = Math.Round(request.Rate, 2, MidpointRounding.AwayFromZero);
        vat.IsEnabled = request.IsEnabled;
        vat.AppliesToDelivery = request.AppliesToDelivery;
        vat.Touch();
        await db.SaveChangesAsync(ct);
        return ToDto(vat);
    }

    private static VatSettingDto ToDto(VatSetting v) => new() { Rate = v.Rate, IsEnabled = v.IsEnabled, AppliesToDelivery = v.AppliesToDelivery };
}
