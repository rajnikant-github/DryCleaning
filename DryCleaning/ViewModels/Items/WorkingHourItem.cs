using CommunityToolkit.Mvvm.ComponentModel;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

/// <summary>One weekday row on the "Working days &amp; hours" screen.</summary>
public sealed partial class WorkingHourItem : ObservableObject
{
    public WorkingHourItem(WorkingHour hour)
    {
        Day = hour.Day;
        DayName = hour.Day.ToString();
        IsOpen = hour.IsOpen;
        OpenTime = hour.Open;
        CloseTime = hour.Close;
    }

    public DayOfWeek Day { get; }
    public string DayName { get; }

    [ObservableProperty]
    private bool isOpen;

    [ObservableProperty]
    private TimeSpan openTime;

    [ObservableProperty]
    private TimeSpan closeTime;

    public WorkingHour ToModel() => new() { Day = Day, IsOpen = IsOpen, Open = OpenTime, Close = CloseTime };
}
