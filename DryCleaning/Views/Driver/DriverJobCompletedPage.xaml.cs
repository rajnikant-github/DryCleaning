using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverJobCompletedPage : BasePage
{
    public DriverJobCompletedPage(DriverJobCompletedViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
