namespace DryCleaning.Api.Domain;

/// <summary>
/// A document the admin asks providers or drivers for (e.g. "Driver's licence"). Configured in the admin portal.
/// </summary>
public class DocumentType : EntityBase
{
    /// <summary>Who must upload it: <see cref="UserRole.Provider"/> or <see cref="UserRole.Driver"/>.</summary>
    public UserRole Role { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>True = the account can't be approved until this document is verified.</summary>
    public bool IsRequired { get; set; }

    /// <summary>When set, the applicant also types a number for this document, e.g. "Licence number". Null = no number.</summary>
    public string? NumberLabel { get; set; }

    /// <summary>Optional regular expression the number must match, e.g. 13 digits for a South African ID.</summary>
    public string? NumberPattern { get; set; }

    /// <summary>True = the applicant also gives the document's expiry date.</summary>
    public bool HasExpiry { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
