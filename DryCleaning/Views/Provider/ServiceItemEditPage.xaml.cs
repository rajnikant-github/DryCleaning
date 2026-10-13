using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class ServiceItemEditPage : BasePage
{
    public ServiceItemEditPage(ServiceItemEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
