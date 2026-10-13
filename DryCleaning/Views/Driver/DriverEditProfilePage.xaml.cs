using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverEditProfilePage : BasePage
{
    public DriverEditProfilePage(DriverEditProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
