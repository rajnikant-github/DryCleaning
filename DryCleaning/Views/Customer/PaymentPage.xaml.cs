using DryCleaning.ViewModels.Customer;

namespace DryCleaning.Views.Customer;

public partial class PaymentPage : BasePage
{
    public PaymentPage(PaymentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
