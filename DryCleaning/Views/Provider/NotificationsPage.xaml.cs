using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class NotificationsPage : BasePage
{
    public NotificationsPage(NotificationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
