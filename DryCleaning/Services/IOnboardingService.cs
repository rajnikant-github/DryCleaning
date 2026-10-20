namespace DryCleaning.Services;

/// <summary>Remembers on this phone whether the intro and onboarding screens have been seen.</summary>
public interface IOnboardingService
{
    bool IsDone { get; }

    /// <summary>Call when the user finishes, skips or leaves the onboarding for the login (so it isn't shown on every launch).</summary>
    void MarkDone();
}
