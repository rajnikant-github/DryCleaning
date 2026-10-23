using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Services;

/// <summary>What a driver is paid per job (one row). Created with 35 on the first read.</summary>
public sealed class DriverPayService(AppDbContext db)
{
    public async Task<DriverPaySetting> CurrentAsync(CancellationToken ct)
    {
        var pay = await db.DriverPaySettings.OrderBy(p => p.CreatedAt).FirstOrDefaultAsync(ct);
        if (pay is not null) return pay;

        pay = new DriverPaySetting();
        db.DriverPaySettings.Add(pay);
        await db.SaveChangesAsync(ct);
        return pay;
    }

    /// <summary>The amount a job created now will pay.</summary>
    public async Task<decimal> CurrentRateAsync(CancellationToken ct) => (await CurrentAsync(ct)).PayPerJob;

    public async Task<DriverPaySettingDto> GetAsync(CancellationToken ct) => ToDto(await CurrentAsync(ct));

    /// <summary>Applies to jobs created from now on; existing jobs keep the pay they were created with.</summary>
    public async Task<DriverPaySettingDto> UpdateAsync(DriverPaySettingDto request, CancellationToken ct)
    {
        var pay = await CurrentAsync(ct);
        pay.PayPerJob = Math.Round(request.PayPerJob, 2, MidpointRounding.AwayFromZero);
        pay.Touch();
        await db.SaveChangesAsync(ct);
        return ToDto(pay);
    }

    private static DriverPaySettingDto ToDto(DriverPaySetting p) => new() { PayPerJob = p.PayPerJob };
}
