using System.Globalization;

namespace DryCleaning.Models;

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

    // Invariant culture: the label is stored on the order and parsed by the API, so it must not follow the phone's locale.
    public static string Label((TimeSpan Start, TimeSpan End) slot) =>
        $"{DateTime.Today.Add(slot.Start).ToString("hh:mm tt", CultureInfo.InvariantCulture)} – {DateTime.Today.Add(slot.End).ToString("hh:mm tt", CultureInfo.InvariantCulture)}";

    /// <summary>True when the provider is open that weekday and the whole window falls inside their hours.</summary>
    public static bool IsWithinHours((TimeSpan Start, TimeSpan End) slot, IReadOnlyList<WorkingHour> hours, DateTime date)
    {
        var day = hours.FirstOrDefault(h => h.Day == date.DayOfWeek);
        return day is { IsOpen: true } && slot.Start >= day.Open && slot.End <= day.Close;
    }
}
