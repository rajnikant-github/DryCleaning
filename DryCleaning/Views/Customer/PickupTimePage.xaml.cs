using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class PickupTimePage : BasePage
{
    public PickupTimePage(PickupTimeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
