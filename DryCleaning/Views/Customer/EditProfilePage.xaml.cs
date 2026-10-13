using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class EditProfilePage : BasePage
{
    public EditProfilePage(EditProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
