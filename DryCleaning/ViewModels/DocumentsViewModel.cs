using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels;

/// <summary>The list of verification documents the admin asks for, with the state of each. Tap one to upload or replace it.</summary>
public sealed partial class DocumentsViewModel(ViewModelServices services, IVerificationDocumentService documents) : BaseViewModel(services)
{
    public ObservableCollection<DocumentRowItem> Rows { get; } = [];

    [ObservableProperty] private string summary = string.Empty;

    // A verification or rejection arrives as a notification: refresh the list straight away.
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.NotificationReceived || message.Kind == RealtimeChangeKind.Reconnected;

    protected override async Task<bool> LoadDataAsync()
    {
        var overview = await documents.GetAsync();
        Rows.Clear();
        foreach (var item in overview.Items)
            Rows.Add(new DocumentRowItem(item, OpenAsync));
        Summary = DocumentTexts.Summary(overview);
        return Rows.Count > 0;
    }

    private Task OpenAsync(DocumentRequirement requirement) =>
        Navigation.GoToAsync(Routes.Document, Param(Routes.TypeIdKey, requirement.TypeId));
}
