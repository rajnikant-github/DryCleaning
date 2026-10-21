using DryCleaning.ViewModels;

namespace DryCleaning.Views;

public partial class DocumentsPage : BasePage
{
    public DocumentsPage(DocumentsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
