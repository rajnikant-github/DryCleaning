namespace DryCleaning.Api.Domain;

/// <summary>One weekday of a provider's pickup hours. A provider without rows is open every day 09:00–19:00.</summary>
public class ProviderWorkingHour : EntityBase
{
    public Guid ServiceProviderId { get; set; }
    public DayOfWeek Day { get; set; }
    public bool IsOpen { get; set; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }

    public Provider ServiceProvider { get; set; } = null!;
}
