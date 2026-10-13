using DryCleaning.ViewModels.Driver;

namespace DryCleaning.Views.Driver;

public partial class DriverJobDetailPage : BasePage
{
    public DriverJobDetailPage(DriverJobDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
