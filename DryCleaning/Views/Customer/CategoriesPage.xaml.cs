using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class CategoriesPage : BasePage
{
    public CategoriesPage(CategoriesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
