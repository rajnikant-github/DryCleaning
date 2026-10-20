namespace DryCleaning.Models;

/// <summary>One weekday of a provider's pickup hours.</summary>
public sealed class WorkingHour
{
    public DayOfWeek Day { get; set; }
    public bool IsOpen { get; set; } = true;
    public TimeSpan Open { get; set; } = new(9, 0, 0);
    public TimeSpan Close { get; set; } = new(19, 0, 0);

    /// <summary>Seven days, Sunday first, open 09:00–19:00 – what a provider has until they set their own hours.</summary>
    public static List<WorkingHour> DefaultWeek() =>
        Enumerable.Range(0, 7).Select(d => new WorkingHour { Day = (DayOfWeek)d }).ToList();
}
