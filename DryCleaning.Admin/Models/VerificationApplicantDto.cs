namespace DryCleaning.Admin.Models;

public sealed class VerificationApplicantDto
{
    public Guid UserId { get; set; }
    public Guid? ProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public string? BusinessName { get; set; }
    public string? LicenseNumber { get; set; }
    public ProfileStatus ProfileStatus { get; set; }
    public List<ReviewDocumentDto> Documents { get; set; } = [];
    public int RequiredCount { get; set; }
    public int VerifiedRequiredCount { get; set; }
    public bool CanApprove { get; set; }
}
