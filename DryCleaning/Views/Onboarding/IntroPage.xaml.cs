using DryCleaning.ViewModels.Onboarding;

namespace DryCleaning.Views.Onboarding;

public partial class IntroPage : BasePage
{
    public IntroPage(IntroViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
