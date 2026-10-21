using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class DocumentTypeRequest
{
    /// <summary><c>Provider</c> or <c>Driver</c>.</summary>
    public UserRole Role { get; set; } = UserRole.Driver;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }

    /// <summary>When set, the applicant also types a number, e.g. "Licence number".</summary>
    public string? NumberLabel { get; set; }

    /// <summary>Optional regular expression the number must match.</summary>
    public string? NumberPattern { get; set; }

    public bool HasExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
