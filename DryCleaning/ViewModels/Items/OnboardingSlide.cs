namespace DryCleaning.ViewModels.Items;

/// <summary>One page of the onboarding carousel.</summary>
public sealed class OnboardingSlide(string title, string description, string image)
{
    public string Title { get; } = title;
    public string Description { get; } = description;
    public string Image { get; } = image;
}
