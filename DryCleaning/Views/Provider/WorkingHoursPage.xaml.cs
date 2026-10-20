using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class WorkingHoursPage : BasePage
{
    public WorkingHoursPage(WorkingHoursViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
