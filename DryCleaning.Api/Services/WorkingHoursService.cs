using System.Globalization;
using System.Text.RegularExpressions;
using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Services;

/// <summary>Provider pickup days and hours, and the check that a customer's pickup slot falls inside them.</summary>
public sealed partial class WorkingHoursService(AppDbContext db, CurrentProfile profile)
{
    private static readonly TimeOnly DefaultOpen = new(9, 0);
    private static readonly TimeOnly DefaultClose = new(19, 0);
    private static readonly TimeSpan MinimumWindow = TimeSpan.FromHours(2);

    public async Task<IReadOnlyList<WorkingHourDto>> GetMineAsync(CancellationToken ct) =>
        await GetAsync((await profile.ProviderAsync(ct)).Id, ct);

    /// <summary>Seven days, Sunday first; providers who never set hours get the default week.</summary>
    public async Task<IReadOnlyList<WorkingHourDto>> GetAsync(Guid providerId, CancellationToken ct)
    {
        var rows = await db.ProviderWorkingHours.AsNoTracking().Where(h => h.ServiceProviderId == providerId).ToListAsync(ct);
        return Enumerable.Range(0, 7)
            .Select(day => rows.FirstOrDefault(r => (int)r.Day == day) is { } row
                ? ToDto(row)
                : new WorkingHourDto { Day = day, IsOpen = true, OpenTime = Format(DefaultOpen), CloseTime = Format(DefaultClose) })
            .ToList();
    }

    public async Task<IReadOnlyList<WorkingHourDto>> SetMineAsync(SetWorkingHoursRequest request, CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        var rows = await db.ProviderWorkingHours.Where(h => h.ServiceProviderId == provider.Id).ToListAsync(ct);

        foreach (var day in request.Days)
        {
            var row = rows.FirstOrDefault(r => (int)r.Day == day.Day);
            if (row is null)
            {
                row = new ProviderWorkingHour { ServiceProviderId = provider.Id, Day = (DayOfWeek)day.Day };
                db.ProviderWorkingHours.Add(row);
            }

            row.IsOpen = day.IsOpen;
            row.OpenTime = day.IsOpen ? TimeOnly.ParseExact(day.OpenTime, "HH:mm", CultureInfo.InvariantCulture) : DefaultOpen;
            row.CloseTime = day.IsOpen ? TimeOnly.ParseExact(day.CloseTime, "HH:mm", CultureInfo.InvariantCulture) : DefaultClose;
            row.Touch();
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(provider.Id, ct);
    }

    /// <summary>Throws when the provider is closed on that day or the slot (e.g. "05:00 PM – 07:00 PM") is outside their hours.</summary>
    public async Task EnsurePickupAllowedAsync(Guid providerId, DateOnly date, string? slot, CancellationToken ct)
    {
        var hours = (await GetAsync(providerId, ct)).First(h => h.Day == (int)date.DayOfWeek);
        if (!hours.IsOpen)
            throw new BadRequestException($"This provider doesn't offer pickups on {date.DayOfWeek}s. Choose another day.");

        if (!TryParseSlot(slot, out var start, out var end))
            return;

        var open = TimeOnly.ParseExact(hours.OpenTime, "HH:mm", CultureInfo.InvariantCulture);
        var close = TimeOnly.ParseExact(hours.CloseTime, "HH:mm", CultureInfo.InvariantCulture);
        if (start < open || end > close)
            throw new BadRequestException($"This provider offers pickups between {hours.OpenTime} and {hours.CloseTime} on {date.DayOfWeek}s.");
    }

    public static bool IsValidWindow(string open, string close) =>
        TimeOnly.TryParseExact(open, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var o) &&
        TimeOnly.TryParseExact(close, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var c) &&
        c - o >= MinimumWindow;

    private static bool TryParseSlot(string? slot, out TimeOnly start, out TimeOnly end)
    {
        start = end = default;
        if (string.IsNullOrWhiteSpace(slot))
            return false;

        var matches = SlotTime().Matches(slot);
        return matches.Count == 2 && TryTime(matches[0], out start) && TryTime(matches[1], out end);
    }

    private static bool TryTime(Match m, out TimeOnly time)
    {
        time = default;
        var hour = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var minute = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        var marker = m.Groups[3].Value.ToUpperInvariant();
        if (marker == "PM" && hour < 12) hour += 12;
        if (marker == "AM" && hour == 12) hour = 0;
        if (hour > 23 || minute > 59) return false;
        time = new TimeOnly(hour, minute);
        return true;
    }

    private static WorkingHourDto ToDto(ProviderWorkingHour h) =>
        new() { Day = (int)h.Day, IsOpen = h.IsOpen, OpenTime = Format(h.OpenTime), CloseTime = Format(h.CloseTime) };

    private static string Format(TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"(\d{1,2}):(\d{2})\s*(AM|PM)?", RegexOptions.IgnoreCase)]
    private static partial Regex SlotTime();
}
