using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D06 – Package photo + customer's pickup code → PickedUp.</summary>
public sealed partial class DriverConfirmPickupViewModel(
    ViewModelServices services, IDriverService drivers, DriverPickupDraft draft, IPhotoService photos)
    : DriverJobViewModelBase(services, drivers)
{
    public const int CodeLength = 4;

    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string code = string.Empty;

    [ObservableProperty] private string summaryText = string.Empty;
    [ObservableProperty] private bool hasCodeError;
    [ObservableProperty] private string codeError = string.Empty;

    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        Title = "Confirm Pickup";
        IReadOnlyList<DriverItemCheck> checks = draft.JobId == JobId ? draft.Items : Array.Empty<DriverItemCheck>();
        var issues = checks.Count(c => c.Condition != ItemCondition.Good || c.CountedQuantity != c.ExpectedQuantity);
        SummaryText = checks.Count == 0
            ? $"{ItemCountText} for {d.ProviderName}"
            : $"{checks.Sum(c => c.CountedQuantity)} of {checks.Sum(c => c.ExpectedQuantity)} items counted" +
              (issues == 0 ? " · all in good condition" : $" · {issues} issue{(issues == 1 ? "" : "s")} noted");
    }

    partial void OnCodeChanged(string value) => HasCodeError = false;

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await photos.AddPhotoAsync();
        if (path is not null)
            Photos.Add(path);
    }

    private bool CanConfirm() => Code.Length == CodeLength;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private async Task ConfirmAsync()
    {
        if (IsBusy)
            return;

        if (Photos.Count == 0 &&
            !await Dialogs.ConfirmAsync("No photo", "A photo of the sealed bag protects you if items are disputed later. Continue without one?", "Continue", "Add photo"))
            return;

        try
        {
            IsBusy = true;
            await Drivers.ConfirmPickupAsync(JobId, new DriverPickupRequest
            {
                PickupCode = Code,
                Items = draft.JobId == JobId ? draft.Items : Array.Empty<DriverItemCheck>(),
                Notes = draft.JobId == JobId ? draft.Notes : null,
                PhotoPaths = Photos.ToList()
            });
            draft.Reset(Guid.Empty);
            // Replace Confirm Pickup (and Verify Items) with the success screen.
            await ReplaceWithAsync(Routes.DriverPickupComplete, pop: 2);
        }
        catch (InvalidOperationException ex)
        {
            CodeError = ex.Message;
            HasCodeError = true;
            Code = string.Empty;
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync("Couldn't confirm pickup", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
