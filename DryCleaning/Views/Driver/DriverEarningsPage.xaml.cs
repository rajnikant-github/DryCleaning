using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverEarningsPage : BasePage
{
    public DriverEarningsPage(DriverEarningsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
