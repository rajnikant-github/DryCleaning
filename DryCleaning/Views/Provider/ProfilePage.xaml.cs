using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class ProfilePage : BasePage
{
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
