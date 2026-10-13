using DryCleaning.ViewModels.Auth;

namespace DryCleaning.Views.Auth;

public partial class ChangePasswordPage : BasePage
{
    public ChangePasswordPage(ChangePasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
