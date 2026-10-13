using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class OrderConfirmedPage : BasePage
{
    public OrderConfirmedPage(OrderConfirmedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
