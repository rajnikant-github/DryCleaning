namespace DryCleaning.Services;

public sealed class OnboardingService(IPreferences preferences) : IOnboardingService
{
    private const string Key = "onboarding_done";

    public bool IsDone => preferences.Get(Key, false);

    public void MarkDone() => preferences.Set(Key, true);
}
