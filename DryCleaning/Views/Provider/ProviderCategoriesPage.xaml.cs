using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class ProviderCategoriesPage : BasePage
{
    public ProviderCategoriesPage(ProviderCategoriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
