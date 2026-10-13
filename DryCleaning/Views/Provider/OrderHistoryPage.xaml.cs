using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class OrderHistoryPage : BasePage
{
    public OrderHistoryPage(OrderHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
