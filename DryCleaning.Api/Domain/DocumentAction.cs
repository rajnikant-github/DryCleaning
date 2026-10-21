namespace DryCleaning.Api.Domain;

/// <summary>What happened to a verification document (kept as an audit trail).</summary>
public enum DocumentAction
{
    Submitted,
    Verified,
    Rejected,

    /// <summary>An admin opened the file.</summary>
    Viewed
}
