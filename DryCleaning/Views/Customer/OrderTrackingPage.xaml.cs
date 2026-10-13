using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class OrderTrackingPage : BasePage
{
    public OrderTrackingPage(OrderTrackingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
