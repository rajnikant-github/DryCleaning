using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class ProcessingPage : BasePage
{
    public ProcessingPage(ProcessingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
