using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class ItemDetailsPage : BasePage
{
    public ItemDetailsPage(ItemDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
