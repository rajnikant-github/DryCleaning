namespace DryCleaning.Api.Dtos;

public sealed class SetWorkingHoursRequest
{
    public List<WorkingHourDto> Days { get; set; } = [];
}
