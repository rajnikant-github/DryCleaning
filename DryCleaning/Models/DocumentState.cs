namespace DryCleaning.Models;

/// <summary>Where one requested verification document stands for the signed-in provider or driver.</summary>
public enum DocumentState
{
    NotSubmitted,
    Pending,
    Verified,
    Rejected
}
