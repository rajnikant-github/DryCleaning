using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class CustomerOrderDetailPage : BasePage
{
    public CustomerOrderDetailPage(CustomerOrderDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
