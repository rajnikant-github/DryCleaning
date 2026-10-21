using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

/// <summary>One row in the "Verification documents" list on a provider's or driver's Profile page.</summary>
public sealed class DocumentRowItem(DocumentRequirement requirement, Func<DocumentRequirement, Task> open)
{
    public DocumentRequirement Requirement { get; } = requirement;
    public string Name => Requirement.Name;
    public string RequiredText => Requirement.IsRequired ? "Required" : "Optional";

    /// <summary>The line under the name: the admin's message when rejected, otherwise the number the applicant entered.</summary>
    public string Detail => Requirement.State switch
    {
        DocumentState.Rejected => Requirement.RejectionMessage ?? "Rejected. Please upload a new one.",
        DocumentState.NotSubmitted => RequiredText,
        _ => string.Join(" · ", new[] { Requirement.Number, Requirement.ExpiresOn is { } e ? $"expires {e:d MMM yyyy}" : null, Requirement.IsRequired ? null : "optional" }.Where(x => !string.IsNullOrEmpty(x)))
    };

    public bool HasDetail => !string.IsNullOrEmpty(Detail);
    public bool IsRejected => Requirement.State == DocumentState.Rejected;

    public string StatusText => Requirement.State switch
    {
        DocumentState.Pending => "In review",
        DocumentState.Verified => "Verified",
        DocumentState.Rejected => "Rejected",
        _ => "Upload"
    };

    public Color StatusBackground => Requirement.State switch
    {
        DocumentState.Pending => Color.FromArgb("#FFFBEB"),
        DocumentState.Verified => Color.FromArgb("#ECFDF5"),
        DocumentState.Rejected => Color.FromArgb("#FEF2F2"),
        _ => Color.FromArgb("#F3F4F6")
    };

    public Color StatusTextColor => Requirement.State switch
    {
        DocumentState.Pending => Color.FromArgb("#92400E"),
        DocumentState.Verified => Color.FromArgb("#047857"),
        DocumentState.Rejected => Color.FromArgb("#B91C1C"),
        _ => Color.FromArgb("#4B5563")
    };

    public Color DetailColor => IsRejected ? Color.FromArgb("#B91C1C") : Color.FromArgb("#6B7280");

    public System.Windows.Input.ICommand OpenCommand { get; } = new Command(async () => await open(requirement));
}
