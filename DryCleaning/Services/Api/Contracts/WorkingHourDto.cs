namespace DryCleaning.Services.Api.Contracts;

/// <summary>One weekday of pickup hours. Day is 0 = Sunday … 6 = Saturday; times are "HH:mm".</summary>
public sealed class WorkingHourDto
{
    public int Day { get; set; }
    public bool IsOpen { get; set; }
    public string OpenTime { get; set; } = "09:00";
    public string CloseTime { get; set; } = "19:00";
}
