using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class ReceiveOrderPage : BasePage
{
    public ReceiveOrderPage(ReceiveOrderViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
