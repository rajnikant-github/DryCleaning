using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class CartPage : BasePage
{
    public CartPage(CartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
