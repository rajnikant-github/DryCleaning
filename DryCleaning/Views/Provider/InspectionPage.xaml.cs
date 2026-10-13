using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class InspectionPage : BasePage
{
    public InspectionPage(InspectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
