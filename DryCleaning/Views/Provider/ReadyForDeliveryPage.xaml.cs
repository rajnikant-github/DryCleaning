using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class ReadyForDeliveryPage : BasePage
{
    public ReadyForDeliveryPage(ReadyForDeliveryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
