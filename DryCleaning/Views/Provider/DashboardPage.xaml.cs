using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class DashboardPage : BasePage
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
