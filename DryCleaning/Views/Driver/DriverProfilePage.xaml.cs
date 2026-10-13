using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverProfilePage : BasePage
{
    public DriverProfilePage(DriverProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
