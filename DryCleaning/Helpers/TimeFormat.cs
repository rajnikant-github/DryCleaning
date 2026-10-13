using DryCleaning.Models;

namespace DryCleaning.Helpers;

public static class TimeFormat
{
    public static string Relative(DateTime at)
    {
        var span = DateTime.Now - at;
        if (span.TotalMinutes < 1) return "Just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24 && at.Date == DateTime.Today) return $"{(int)span.TotalHours} h ago";
        if (at.Date == DateTime.Today.AddDays(-1)) return $"Yesterday, {at:HH:mm}";
        return at.ToString("d MMM, HH:mm");
    }

    public static string Greeting(DateTime now) => now.Hour switch
    {
        < 12 => "Good morning",
        < 17 => "Good afternoon",
        _ => "Good evening"
    };
}
