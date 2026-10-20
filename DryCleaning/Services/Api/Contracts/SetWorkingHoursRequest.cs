namespace DryCleaning.Services.Api.Contracts;

/// <summary>PUT api/provider/working-hours.</summary>
public sealed class SetWorkingHoursRequest
{
    public List<WorkingHourDto> Days { get; set; } = [];
}
