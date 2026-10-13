using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class ProductDetailPage : BasePage
{
    public ProductDetailPage(ProductDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
