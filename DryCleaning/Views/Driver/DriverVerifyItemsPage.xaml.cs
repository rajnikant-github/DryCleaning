using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverVerifyItemsPage : BasePage
{
    public DriverVerifyItemsPage(DriverVerifyItemsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
