using DryCleaning.ViewModels.Auth;

namespace DryCleaning.Views.Auth;

public partial class SplashPage : BasePage
{
    public SplashPage(SplashViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
