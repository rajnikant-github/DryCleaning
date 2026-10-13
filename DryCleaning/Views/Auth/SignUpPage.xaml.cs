using DryCleaning.ViewModels.Auth;

namespace DryCleaning.Views.Auth;

public partial class SignUpPage : BasePage
{
    public SignUpPage(SignUpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
