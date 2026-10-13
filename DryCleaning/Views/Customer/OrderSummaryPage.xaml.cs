using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class OrderSummaryPage : BasePage
{
    public OrderSummaryPage(OrderSummaryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
