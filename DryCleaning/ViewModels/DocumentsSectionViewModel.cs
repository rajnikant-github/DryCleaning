using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Api;

namespace DryCleaning.ViewModels;

/// <summary>
/// The "Verification documents" section on a provider's or driver's Profile page: a one-line summary of where their documents stand.
/// Tapping it opens the full list.
/// </summary>
public sealed partial class DocumentsSectionViewModel(IVerificationDocumentService documents, INavigationService navigation) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVisible))]
    private bool hasDocuments;

    [ObservableProperty] private string summary = string.Empty;

    public bool IsVisible => HasDocuments;

    /// <summary>Loads the summary; a failure (offline) just leaves the section as it was.</summary>
    public async Task LoadAsync()
    {
        try
        {
            var overview = await documents.GetAsync();
            HasDocuments = overview.Items.Count > 0;
            Summary = DocumentTexts.Summary(overview);
        }
        catch (ApiException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Documents] Load failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private Task OpenAsync() => navigation.GoToAsync(Routes.Documents);
}
