using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class ProductEditPage : BasePage
{
    public ProductEditPage(ProductEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
