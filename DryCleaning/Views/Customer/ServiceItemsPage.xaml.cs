using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class ServiceItemsPage : BasePage
{
    public ServiceItemsPage(ServiceItemsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
