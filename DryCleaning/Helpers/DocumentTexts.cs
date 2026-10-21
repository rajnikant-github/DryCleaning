using DryCleaning.Models;

namespace DryCleaning.Helpers;

/// <summary>Wording for the documents summary line (Profile section and the Documents page).</summary>
public static class DocumentTexts
{
    public static string Summary(DocumentsOverview overview)
    {
        var text = overview.RequiredCount == 0
            ? "Upload the documents below so our team can verify you."
            : overview.AllRequiredVerified
                ? "All required documents are verified."
                : overview.ActionNeededCount > 0
                    ? $"{overview.VerifiedRequiredCount} of {overview.RequiredCount} required documents verified. {overview.ActionNeededCount} still needed from you."
                    : $"{overview.VerifiedRequiredCount} of {overview.RequiredCount} required documents verified. The rest are in review.";

        // Optional documents don't hold up approval, but say where they stand so "all verified" isn't misleading.
        var optionalInReview = overview.Items.Count(i => !i.IsRequired && i.State == DocumentState.Pending);
        var optionalRejected = overview.Items.Count(i => !i.IsRequired && i.State == DocumentState.Rejected);
        if (optionalInReview > 0)
            text += $" {optionalInReview} optional {(optionalInReview == 1 ? "document is" : "documents are")} still in review.";
        if (optionalRejected > 0)
            text += $" {optionalRejected} optional {(optionalRejected == 1 ? "document needs" : "documents need")} a new upload.";
        return text;
    }
}
