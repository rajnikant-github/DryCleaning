using DryCleaning.ViewModels;

namespace DryCleaning.Views;

public partial class DocumentPage : BasePage
{
    public DocumentPage(DocumentViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
