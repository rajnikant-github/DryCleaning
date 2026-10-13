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

    public static string Label((TimeSpan Start, TimeSpan End) slot) =>
        $"{DateTime.Today.Add(slot.Start):hh:mm tt} – {DateTime.Today.Add(slot.End):hh:mm tt}";
}
