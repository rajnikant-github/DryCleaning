using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class CustomerHomePage : BasePage
{
    public CustomerHomePage(CustomerHomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
