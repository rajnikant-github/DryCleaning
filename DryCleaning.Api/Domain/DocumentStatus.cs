namespace DryCleaning.Api.Domain;

/// <summary>Where an uploaded verification document stands. A document that was never uploaded has no row (the API reports "NotSubmitted").</summary>
public enum DocumentStatus
{
    /// <summary>Uploaded and waiting for an admin.</summary>
    Pending,

    /// <summary>An admin checked it and it is fine.</summary>
    Verified,

    /// <summary>An admin rejected it (with a message); the applicant must upload a new one.</summary>
    Rejected
}
