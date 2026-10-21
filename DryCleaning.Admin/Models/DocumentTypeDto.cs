namespace DryCleaning.Admin.Models;

public sealed class DocumentTypeDto
{
    public Guid Id { get; set; }
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public string? NumberLabel { get; set; }
    public string? NumberPattern { get; set; }
    public bool HasExpiry { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}
