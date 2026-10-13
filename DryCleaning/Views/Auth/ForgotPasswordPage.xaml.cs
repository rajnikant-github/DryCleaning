using DryCleaning.ViewModels.Auth;

namespace DryCleaning.Views.Auth;

public partial class ForgotPasswordPage : BasePage
{
    public ForgotPasswordPage(ForgotPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
