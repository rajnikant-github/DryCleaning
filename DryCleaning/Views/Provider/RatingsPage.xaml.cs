using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class RatingsPage : BasePage
{
    public RatingsPage(RatingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
