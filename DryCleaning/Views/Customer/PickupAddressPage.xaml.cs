using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class PickupAddressPage : BasePage
{
    public PickupAddressPage(PickupAddressViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
