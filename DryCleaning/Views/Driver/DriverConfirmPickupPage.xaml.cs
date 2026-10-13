using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverConfirmPickupPage : BasePage
{
    public DriverConfirmPickupPage(DriverConfirmPickupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
