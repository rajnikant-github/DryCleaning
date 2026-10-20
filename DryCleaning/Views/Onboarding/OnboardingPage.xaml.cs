using DryCleaning.ViewModels.Onboarding;

namespace DryCleaning.Views.Onboarding;

public partial class OnboardingPage : BasePage
{
    public OnboardingPage(OnboardingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
