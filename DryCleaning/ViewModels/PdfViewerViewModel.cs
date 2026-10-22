using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;
using MauiNativePdfView.Abstractions;

namespace DryCleaning.ViewModels;

/// <summary>Shows the PDF I uploaded inside the app. If it can't be shown, the web-browser preview is offered instead.</summary>
public sealed partial class PdfViewerViewModel(ViewModelServices services, IVerificationDocumentService documents)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid _typeId;
    private string _fileName = "document.pdf";

    [ObservableProperty] private string title = "Document";
    [ObservableProperty] private PdfSource? source;
    [ObservableProperty] private int currentPage;
    [ObservableProperty] private int pageCount;
    [ObservableProperty] private bool hasFailed;
    [ObservableProperty] private string failureMessage = string.Empty;

    public string PageText => PageCount > 0 ? $"Page {CurrentPage + 1} of {PageCount}" : string.Empty;

    partial void OnCurrentPageChanged(int value) => OnPropertyChanged(nameof(PageText));
    partial void OnPageCountChanged(int value) => OnPropertyChanged(nameof(PageText));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.TypeIdKey, out var id) && Guid.TryParse(id?.ToString(), out var typeId))
            _typeId = typeId;
        if (query.TryGetValue(Routes.FileNameKey, out var name) && !string.IsNullOrWhiteSpace(name?.ToString()))
            _fileName = name.ToString()!;
        if (query.TryGetValue(Routes.TitleKey, out var title) && !string.IsNullOrWhiteSpace(title?.ToString()))
            Title = title.ToString()!;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        HasFailed = false;
        try
        {
            // The file is downloaded with my sign-in (documents are never public) and shown from the phone's storage.
            var path = await documents.DownloadAsync(_typeId, _fileName);
            Source = PdfSource.FromFile(path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[PdfViewer] Could not load the PDF: {ex.Message}");
            FailureMessage = "We couldn't show this PDF here.";
            HasFailed = true;
        }
        return true;
    }

    /// <summary>The PDF viewer reported a problem (damaged file, wrong format).</summary>
    public void OnViewerError(string? message)
    {
        System.Diagnostics.Debug.WriteLine($"[PdfViewer] Viewer error: {message}");
        FailureMessage = "We couldn't show this PDF here.";
        HasFailed = true;
    }

    [RelayCommand]
    private Task OpenInBrowserAsync() => RunBusyAsync(async () =>
    {
        var link = await documents.GetPreviewLinkAsync(_typeId);
        await Browser.Default.OpenAsync(link, BrowserLaunchMode.External);
    });
}
