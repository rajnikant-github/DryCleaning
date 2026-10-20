namespace DryCleaning.Api.Dtos;

/// <summary>One weekday of a provider's pickup hours. Times are "HH:mm" (24-hour); Day is 0 = Sunday … 6 = Saturday.</summary>
public sealed class WorkingHourDto
{
    public int Day { get; set; }
    public bool IsOpen { get; set; }
    public string OpenTime { get; set; } = "09:00";
    public string CloseTime { get; set; } = "19:00";
}
