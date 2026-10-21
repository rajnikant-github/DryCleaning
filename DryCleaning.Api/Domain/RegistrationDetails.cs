namespace DryCleaning.Api.Domain;

/// <summary>The role-specific part of a pending sign-up.</summary>
public class RegistrationDetails
{
    public string? BusinessName { get; set; }
    public string? Description { get; set; }
    public List<Guid> CategoryIds { get; set; } = [];
    public string? LicenseNumber { get; set; }
}
