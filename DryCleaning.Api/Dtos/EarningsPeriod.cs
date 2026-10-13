using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public enum EarningsPeriod
{
    /// <summary>Last 7 days, one bar per day.</summary>
    Week,
    /// <summary>Last 4 weeks, one bar per week.</summary>
    Month
}
