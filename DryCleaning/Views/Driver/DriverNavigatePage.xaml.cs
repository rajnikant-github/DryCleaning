using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverNavigatePage : BasePage
{
    public DriverNavigatePage(DriverNavigateViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
