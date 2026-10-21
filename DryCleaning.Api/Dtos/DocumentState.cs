namespace DryCleaning.Api.Dtos;

/// <summary>What the applicant sees for one requested document.</summary>
public enum DocumentState
{
    /// <summary>Nothing uploaded yet.</summary>
    NotSubmitted,

    /// <summary>Uploaded, waiting for an admin.</summary>
    Pending,

    Verified,

    /// <summary>Rejected: see the message and upload a new file.</summary>
    Rejected
}
