using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverDashboardPage : BasePage
{
    public DriverDashboardPage(DriverDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
