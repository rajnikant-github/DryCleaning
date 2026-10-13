using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class ProductsPage : BasePage
{
    public ProductsPage(ProductsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
