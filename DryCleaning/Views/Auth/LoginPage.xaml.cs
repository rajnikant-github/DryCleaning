using DryCleaning.ViewModels.Auth;

namespace DryCleaning.Views.Auth;

public partial class LoginPage : BasePage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
