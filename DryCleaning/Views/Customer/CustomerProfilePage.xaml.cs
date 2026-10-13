using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class CustomerProfilePage : BasePage
{
    public CustomerProfilePage(CustomerProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
