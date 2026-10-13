using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D05 – Count items and note their condition with the customer.</summary>
public sealed partial class DriverVerifyItemsViewModel(ViewModelServices services, IDriverService drivers, DriverPickupDraft draft)
    : DriverJobViewModelBase(services, drivers)
{
    public ObservableCollection<DriverItemCheckItem> Items { get; } = [];

    [ObservableProperty] private string countedText = string.Empty;
    [ObservableProperty] private bool hasIssues;
    [ObservableProperty] private string notes = string.Empty;

    // Keep the driver's counts when the page re-appears (e.g. after the camera).
    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        Title = "Verify Items";
        Items.Clear();
        foreach (var item in d.Items)
        {
            var check = new DriverItemCheckItem(item);
            check.PropertyChanged += (_, _) => UpdateTotals();
            Items.Add(check);
        }
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        var expected = Items.Sum(i => i.ExpectedQuantity);
        var counted = Items.Sum(i => i.CountedQuantity);
        CountedText = $"{counted} of {expected} items counted";
        HasIssues = Items.Any(i => i.HasDiscrepancy || !i.IsGood);
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        if (HasIssues && !await Dialogs.ConfirmAsync("Items don't match",
                "Some items are missing, extra or not in good condition. They'll be noted on the order for the provider and customer.",
                "Continue", "Recount"))
            return;

        draft.Reset(JobId);
        draft.Items = Items.Select(i => i.ToCheck()).ToList();
        draft.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        await Navigation.GoToAsync(Routes.DriverConfirmPickup, Param(Routes.JobIdKey, JobId));
    }
}
