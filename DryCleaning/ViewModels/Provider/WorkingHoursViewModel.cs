using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Set the days and hours customers can book pickups for this provider.</summary>
public sealed partial class WorkingHoursViewModel(ViewModelServices services, IProviderProfileService profiles)
    : BaseViewModel(services)
{
    private static readonly TimeSpan MinimumWindow = TimeSpan.FromHours(2);

    public ObservableCollection<WorkingHourItem> Days { get; } = [];

    protected override async Task<bool> LoadDataAsync()
    {
        var hours = await profiles.GetWorkingHoursAsync();
        Days.Clear();
        // Monday first, Sunday last.
        foreach (var hour in hours.OrderBy(h => ((int)h.Day + 6) % 7))
            Days.Add(new WorkingHourItem(hour));
        return Days.Count > 0;
    }

    // Load once – keep the user's changes when the page re-appears.
    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    [RelayCommand]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        if (!Days.Any(d => d.IsOpen))
        {
            await Dialogs.AlertAsync("Working hours", "Be open on at least one day.");
            return;
        }

        var invalid = Days.FirstOrDefault(d => d.IsOpen && d.CloseTime - d.OpenTime < MinimumWindow);
        if (invalid is not null)
        {
            await Dialogs.AlertAsync("Working hours", $"{invalid.DayName}: open for at least two hours so customers can book a pickup window.");
            return;
        }

        await profiles.SetWorkingHoursAsync(Days.Select(d => d.ToModel()).ToList());
        await Navigation.GoBackAsync();
    });
}
