using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class EditBusinessProfilePage : BasePage
{
    public EditBusinessProfilePage(EditBusinessProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
