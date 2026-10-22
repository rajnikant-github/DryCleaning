using DryCleaning.ViewModels;
using MauiNativePdfView.Abstractions;

namespace DryCleaning.Views;

public partial class PdfViewerPage : BasePage
{
    public PdfViewerPage(PdfViewerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>The viewer could not render the file (damaged or not a real PDF): show the browser option instead.</summary>
    private void OnViewerError(object? sender, PdfErrorEventArgs e) => (BindingContext as PdfViewerViewModel)?.OnViewerError(e.Message);
}
