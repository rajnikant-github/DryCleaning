using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class ServicesPage : BasePage
{
    public ServicesPage(CatalogViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
