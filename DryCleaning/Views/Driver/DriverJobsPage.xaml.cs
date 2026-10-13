using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverJobsPage : BasePage
{
    public DriverJobsPage(DriverJobsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
