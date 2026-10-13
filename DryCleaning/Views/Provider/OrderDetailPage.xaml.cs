using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class OrderDetailPage : BasePage
{
    public OrderDetailPage(OrderDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
