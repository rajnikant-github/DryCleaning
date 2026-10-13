using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverDeliverPage : BasePage
{
    public DriverDeliverPage(DriverDeliverViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
