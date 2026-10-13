using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class OrdersPage : BasePage
{
    public OrdersPage(OrdersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
