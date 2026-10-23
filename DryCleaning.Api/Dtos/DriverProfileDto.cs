using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class DriverProfileDto
{
    public required Guid Id { get; init; }
    public required UserDto User { get; init; }
    public required string LicenseNumber { get; init; }
    public required bool IsAvailable { get; init; }
    public required bool IsActive { get; init; }
    public required AddressDto? Address { get; init; }

    /// <summary>Everything this driver has earned from completed jobs.</summary>
    public required decimal TotalEarnings { get; init; }
}
