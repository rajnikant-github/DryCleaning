using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Phones registered for push notifications (one FCM token per app install).</summary>
public sealed class DeviceService(AppDbContext db, CurrentProfile profile)
{
    private const int MaxDevicesPerUser = 10;

    /// <summary>Adds or refreshes this phone. A token that moves to another account (sign-out/sign-in) is reassigned.</summary>
    public async Task RegisterAsync(RegisterDeviceRequest request, CancellationToken ct)
    {
        var userId = profile.UserId;
        var token = request.Token.Trim();
        var now = DateTime.UtcNow;

        var device = await db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token, ct);
        if (device is null)
        {
            db.DeviceTokens.Add(new DeviceToken { UserId = userId, Token = token, Platform = request.Platform, LastSeenAt = now });
        }
        else
        {
            device.UserId = userId;
            device.Platform = request.Platform;
            device.LastSeenAt = now;
        }

        // Keep the most recent phones only.
        var stale = await db.DeviceTokens
            .Where(d => d.UserId == userId && d.Token != token)
            .OrderByDescending(d => d.LastSeenAt)
            .Skip(MaxDevicesPerUser - 1)
            .ToListAsync(ct);
        db.DeviceTokens.RemoveRange(stale);

        await db.SaveChangesAsync(ct);
    }

    public async Task UnregisterAsync(string token, CancellationToken ct)
    {
        var userId = profile.UserId;
        var value = token.Trim();
        await db.DeviceTokens.Where(d => d.UserId == userId && d.Token == value).ExecuteDeleteAsync(ct);
    }
}
