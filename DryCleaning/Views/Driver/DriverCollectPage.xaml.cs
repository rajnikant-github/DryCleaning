using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverCollectPage : BasePage
{
    public DriverCollectPage(DriverCollectViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
