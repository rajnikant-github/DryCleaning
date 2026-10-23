namespace DryCleaning.Api.Domain;

/// <summary>
/// What the marketplace pays a driver for each completed pickup or delivery job (a single row, edited by admins).
/// A job keeps the amount it was created with, so changing this never re-prices jobs that already exist.
/// </summary>
public class DriverPaySetting : EntityBase
{
    public decimal PayPerJob { get; set; } = 35m;
}
