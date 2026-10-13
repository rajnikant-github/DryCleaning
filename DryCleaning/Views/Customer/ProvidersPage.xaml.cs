using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class ProvidersPage : BasePage
{
    public ProvidersPage(ProvidersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
