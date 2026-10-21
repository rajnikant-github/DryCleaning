using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels;

/// <summary>Upload (or replace) one verification document: the file, plus its number and expiry date when the admin asks for them.</summary>
public sealed partial class DocumentViewModel(ViewModelServices services, IVerificationDocumentService documents, IDocumentPicker picker)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid _typeId;
    private PickedDocument? _picked;
    private long _maxPdfBytes = 50 * 1024 * 1024;
    private long _maxImageBytes = 15 * 1024 * 1024;

    [ObservableProperty] private string title = "Document";
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRejection))]
    private string? rejectionMessage;
    [ObservableProperty] private string numberLabel = string.Empty;
    [ObservableProperty] private string note = string.Empty;

    [ObservableProperty] private bool hasUploadedFile;
    private string? _uploadedFileName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRejection))]
    [NotifyPropertyChangedFor(nameof(HasNote))]
    private DocumentState documentState;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string number = string.Empty;

    [ObservableProperty] private bool hasNumber;
    [ObservableProperty] private bool hasExpiry;
    [ObservableProperty] private DateTime expiresOn = DateTime.Today.AddYears(1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFile))]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string fileText = "No file chosen";

    /// <summary>The label above the file box, with the size limit the server reports.</summary>
    public string FileLabel => $"File (a PDF up to {SizeText.Megabytes(_maxPdfBytes)}, or a photo)";

    public DateTime MinExpiry => DateTime.Today;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public bool HasRejection => DocumentState == DocumentState.Rejected && !string.IsNullOrWhiteSpace(RejectionMessage);
    public bool HasNote => !string.IsNullOrEmpty(Note);
    public bool HasFile => _picked is not null;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.TypeIdKey, out var id))
            _typeId = id;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var overview = await documents.GetAsync();
        _maxPdfBytes = overview.MaxPdfBytes;
        _maxImageBytes = overview.MaxImageBytes;
        OnPropertyChanged(nameof(FileLabel));
        var requirement = overview.Items.FirstOrDefault(i => i.TypeId == _typeId);
        if (requirement is null)
            return false;

        Title = requirement.Name;
        Description = requirement.Description ?? string.Empty;
        OnPropertyChanged(nameof(HasDescription));
        DocumentState = requirement.State;
        HasUploadedFile = requirement.State != DocumentState.NotSubmitted;
        _uploadedFileName = requirement.FileName;
        RejectionMessage = requirement.RejectionMessage;
        NumberLabel = requirement.NumberLabel ?? string.Empty;
        HasNumber = !string.IsNullOrEmpty(requirement.NumberLabel);
        Number = requirement.Number ?? string.Empty;
        HasExpiry = requirement.HasExpiry;
        ExpiresOn = requirement.ExpiresOn is { } date && date >= DateTime.Today ? date : DateTime.Today.AddYears(1);
        OnPropertyChanged(nameof(MinExpiry));

        Note = requirement.State switch
        {
            DocumentState.Verified => "This document is verified. Uploading a new file sends it back for review.",
            DocumentState.Pending => $"Uploaded{(string.IsNullOrEmpty(requirement.FileName) ? "" : $" ({requirement.FileName})")} and waiting for our team. You can replace it if you made a mistake.",
            _ => string.Empty
        };
        OnPropertyChanged(nameof(HasNote));
        return true;
    }

    /// <summary>Opens the file I uploaded earlier in the phone's own viewer, so I can check what I sent.</summary>
    [RelayCommand]
    private Task ViewFileAsync() => RunBusyAsync(async () =>
    {
        // A PDF opens in the web browser through a short-lived private link; if that isn't possible (demo mode, offline hiccup)
        // or the file is a photo, it is downloaded and shown in the phone's own viewer instead.
        if (_uploadedFileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var link = await documents.GetPreviewLinkAsync(_typeId);
                await Browser.Default.OpenAsync(link, BrowserLaunchMode.External);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"[Documents] Browser preview failed, using the phone's viewer: {ex.Message}");
            }
        }

        var path = await documents.DownloadAsync(_typeId, _uploadedFileName ?? "document");
        await Launcher.Default.OpenAsync(new OpenFileRequest(Title, new ReadOnlyFile(path)));
    });

    private bool CanSubmit() => _picked is not null && (!HasNumber || !string.IsNullOrWhiteSpace(Number));

    [RelayCommand]
    private async Task ChooseFileAsync()
    {
        var picked = await picker.PickAsync(_maxPdfBytes, _maxImageBytes);
        if (picked is null)
            return;

        _picked = picked;
        FileText = picked.FileName;
        OnPropertyChanged(nameof(HasFile));
        SubmitCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private Task SubmitAsync() => RunBusyAsync(async () =>
    {
        if (HasExpiry && ExpiresOn.Date < DateTime.Today)
        {
            await Dialogs.AlertAsync("Expiry date", "That document has already expired. Upload a current one.");
            return;
        }

        await documents.SubmitAsync(_typeId, _picked!.Path, HasNumber ? Number.Trim() : null, HasExpiry ? ExpiresOn.Date : null);
        await Dialogs.AlertAsync("Document sent", "Thanks! Our team will check it and let you know.");
        await Navigation.GoBackAsync();
    });
}
