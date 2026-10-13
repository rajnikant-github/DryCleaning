using DryCleaning.ViewModels.Provider;

namespace DryCleaning.Views.Provider;

public partial class QualityCheckPage : BasePage
{
    public QualityCheckPage(QualityCheckViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
