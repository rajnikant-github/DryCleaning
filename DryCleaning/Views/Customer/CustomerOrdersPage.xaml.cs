using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class CustomerOrdersPage : BasePage
{
    public CustomerOrdersPage(CustomerOrdersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
