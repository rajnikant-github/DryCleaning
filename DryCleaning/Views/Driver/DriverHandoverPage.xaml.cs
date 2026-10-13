using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverHandoverPage : BasePage
{
    public DriverHandoverPage(DriverHandoverViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
