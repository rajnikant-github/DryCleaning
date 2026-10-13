using DryCleaning.ViewModels.Auth;

namespace DryCleaning.Views.Auth;

public partial class RoleSelectionPage : BasePage
{
    public RoleSelectionPage(RoleSelectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
