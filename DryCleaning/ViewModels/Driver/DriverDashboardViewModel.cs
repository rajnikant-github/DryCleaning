using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D01 – Driver dashboard: availability, today's numbers and the active job.</summary>
public sealed partial class DriverDashboardViewModel(ViewModelServices services, IDriverService drivers)
    : BaseViewModel(services)
{
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.Reconnected || message.Kind == RealtimeChangeKind.JobChanged;

    private bool _suppressOnline;
    private Guid? _activeJobId;

    public ObservableCollection<DriverJobCardItem> TodaysJobs { get; } = [];

    [ObservableProperty] private string greeting = string.Empty;
    [ObservableProperty] private string driverName = string.Empty;
    [ObservableProperty] private bool hasUnread;
    [ObservableProperty] private int availableCount;
    [ObservableProperty] private int completedToday;
    [ObservableProperty] private decimal earningsToday;
    [ObservableProperty] private bool hasTodaysJobs;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OnlineText))]
    [NotifyPropertyChangedFor(nameof(OnlineHint))]
    [NotifyPropertyChangedFor(nameof(IsDriverOffline))]
    private bool isOnline;

    [ObservableProperty] private bool hasActiveJob;
    [ObservableProperty] private string activeTypeIcon = string.Empty;
    [ObservableProperty] private string activeTitle = string.Empty;
    [ObservableProperty] private string activeStepText = string.Empty;
    [ObservableProperty] private string activeDestination = string.Empty;
    [ObservableProperty] private string activeDistanceText = string.Empty;
    [ObservableProperty] private string activeActionText = string.Empty;
    [ObservableProperty] private string availableText = string.Empty;

    public string OnlineText => IsOnline ? "You're online" : "You're offline";
    public string OnlineHint => IsOnline ? "New pickup and delivery jobs will appear in Jobs." : "Go online to receive new jobs.";
    public bool IsDriverOffline => !IsOnline;

    protected override async Task<bool> LoadDataAsync()
    {
        var dashboard = await drivers.GetDashboardAsync();
        Greeting = TimeFormat.Greeting(DateTime.Now) + ",";
        DriverName = dashboard.DriverName.Split(' ')[0];
        HasUnread = dashboard.UnreadNotifications > 0;
        SetOnlineSilently(dashboard.IsOnline);
        AvailableCount = dashboard.AvailableCount;
        AvailableText = dashboard.AvailableCount switch
        {
            0 => "No jobs waiting right now",
            1 => "1 job waiting for a driver",
            var n => $"{n} jobs waiting for a driver"
        };
        CompletedToday = dashboard.CompletedToday;
        EarningsToday = dashboard.EarningsToday;

        var active = dashboard.ActiveJob;
        _activeJobId = active?.Job.Id;
        HasActiveJob = active is not null;
        if (active is not null)
        {
            ActiveTypeIcon = active.Job.TypeIcon;
            ActiveTitle = $"{active.Job.TypeText} · {active.Job.OrderNumber}";
            ActiveStepText = DriverText.StepTitle(active.NextStep);
            ActiveDestination = $"{active.DestinationName} · {active.Destination.SubLocality ?? active.Destination.Locality}";
            ActiveDistanceText = $"{Geo.DistanceText(active.Job.DistanceKm)} · {active.Job.ItemCount} items";
            ActiveActionText = DriverText.ActionText(active.NextStep);
        }

        TodaysJobs.Clear();
        foreach (var job in dashboard.TodaysJobs)
            TodaysJobs.Add(new DriverJobCardItem(job, OpenJobAsync));
        HasTodaysJobs = TodaysJobs.Count > 0;
        return true;
    }

    private void SetOnlineSilently(bool value)
    {
        _suppressOnline = true;
        IsOnline = value;
        _suppressOnline = false;
    }

    partial void OnIsOnlineChanged(bool value)
    {
        if (!_suppressOnline)
            _ = ChangeOnlineAsync(value);
    }

    private async Task ChangeOnlineAsync(bool value)
    {
        try
        {
            if (!value && HasActiveJob &&
                !await Dialogs.ConfirmAsync("Go offline?", "You still have an active job. You'll stop receiving new jobs, but you must finish this one.", "Go offline", "Stay online"))
            {
                SetOnlineSilently(true);
                return;
            }

            var result = await drivers.SetOnlineAsync(value);
            SetOnlineSilently(result);
            await LoadAsync(showSkeleton: false);
        }
        catch (Exception ex)
        {
            SetOnlineSilently(!value);
            await Dialogs.AlertAsync("Availability", ex.Message);
        }
    }

    private Task OpenJobAsync(DriverJobSummary job) =>
        Navigation.GoToAsync(Routes.DriverJobDetail, Param(Routes.JobIdKey, job.Id));

    [RelayCommand]
    private Task ContinueActiveJobAsync() => _activeJobId is { } id
        ? Navigation.GoToAsync(Routes.DriverNavigate, Param(Routes.JobIdKey, id))
        : Task.CompletedTask;

    [RelayCommand]
    private Task OpenActiveJobAsync() => _activeJobId is { } id
        ? Navigation.GoToAsync(Routes.DriverJobDetail, Param(Routes.JobIdKey, id))
        : Task.CompletedTask;

    [RelayCommand]
    private Task FindJobsAsync() => Navigation.GoToAsync(Routes.DriverJobs, Param(Routes.FilterKey, nameof(DriverJobFilter.Available)));

    [RelayCommand]
    private Task OpenEarningsAsync() => Navigation.GoToAsync(Routes.DriverEarnings);

    [RelayCommand]
    private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);
}
