using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Onboarding;

/// <summary>Three swipeable onboarding slides, with Next / Skip / back and a final Get Started.</summary>
public sealed partial class OnboardingViewModel(ViewModelServices services, IOnboardingService onboarding) : BaseViewModel(services)
{
    public ObservableCollection<OnboardingSlide> Slides { get; } =
    [
        new("Amazing Service & Offers", "Find deals that are cheaper than local laundromats, great discounts, and cash backs.", "onboarding_one.png"),
        new("Peace of mind in your pocket", "Quickly search and select a wide range of trusted and professional laundromat services.", "onboarding_two.png"),
        new("Deliver at Your Door", "Safe and fast delivery for your valuable laundry at your door without any hustle", "onboarding_three.png")
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLast))]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    private int position;

    public bool IsLast => Position >= Slides.Count - 1;
    public string ButtonText => IsLast ? "Get Started" : "Next";

    public override Task OnAppearingAsync()
    {
        State = ViewState.Content;
        Position = 0; // always begin at the first slide, also when arriving back from the intro
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task NextAsync()
    {
        if (IsLast)
            return FinishAsync();

        Position++;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task BackAsync()
    {
        if (Position > 0)
        {
            Position--;
            return Task.CompletedTask;
        }

        return Navigation.GoToAsync(Routes.Intro);
    }

    [RelayCommand]
    private Task SkipAsync() => FinishAsync();

    private Task FinishAsync()
    {
        onboarding.MarkDone();
        return Navigation.GoToAsync(Routes.RoleSelection);
    }
}
