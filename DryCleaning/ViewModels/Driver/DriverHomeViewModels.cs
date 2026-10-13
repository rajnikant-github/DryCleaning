using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D01 – Driver dashboard: availability, today's numbers and the active job.</summary>
public sealed partial class DriverDashboardViewModel(ViewModelServices services, IDriverService drivers)
    : BaseViewModel(services)
{
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

/// <summary>D02 – Jobs: Available (marketplace) / My Jobs / Completed.</summary>
public sealed partial class DriverJobsViewModel(ViewModelServices services, IDriverService drivers)
    : BaseViewModel(services), IQueryAttributable
{
    private DriverJobFilter _filter = DriverJobFilter.Available;

    public ObservableCollection<SegmentItem> Segments { get; } = [];
    public ObservableCollection<DriverJobCardItem> Jobs { get; } = [];

    [ObservableProperty] private string emptyTitle = string.Empty;
    [ObservableProperty] private string emptyMessage = string.Empty;
    [ObservableProperty] private bool showOfflineBanner;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.FilterKey, out var value) &&
            Enum.TryParse<DriverJobFilter>(value?.ToString(), out var filter))
        {
            _filter = filter;
            foreach (var segment in Segments)
                segment.IsSelected = segment.Key == filter.ToString();
        }
    }

    protected override async Task<bool> LoadDataAsync()
    {
        if (Segments.Count == 0)
        {
            Segments.Add(new SegmentItem(nameof(DriverJobFilter.Available), "Available", OnSegmentSelected));
            Segments.Add(new SegmentItem(nameof(DriverJobFilter.Mine), "My Jobs", OnSegmentSelected));
            Segments.Add(new SegmentItem(nameof(DriverJobFilter.Completed), "Completed", OnSegmentSelected));
            foreach (var segment in Segments)
                segment.IsSelected = segment.Key == _filter.ToString();
        }

        var counts = await drivers.GetJobCountsAsync();
        foreach (var segment in Segments)
            segment.Count = counts.TryGetValue(Enum.Parse<DriverJobFilter>(segment.Key), out var c) ? c : 0;

        var dashboard = await drivers.GetDashboardAsync();
        ShowOfflineBanner = _filter == DriverJobFilter.Available && !dashboard.IsOnline;

        var jobs = await drivers.GetJobsAsync(_filter);
        Jobs.Clear();
        foreach (var job in jobs)
            Jobs.Add(new DriverJobCardItem(job, OpenAsync, _filter == DriverJobFilter.Available ? AcceptAsync : null));

        (EmptyTitle, EmptyMessage) = _filter switch
        {
            DriverJobFilter.Available when ShowOfflineBanner => ("You're offline", "Go online to see pickup and delivery jobs near you."),
            DriverJobFilter.Available => ("No jobs right now", "New jobs appear here as soon as providers accept orders. Pull to refresh."),
            DriverJobFilter.Mine => ("No active jobs", "Accept a job from the Available tab to get started."),
            _ => ("No completed jobs yet", "Jobs you finish will show here with your earnings.")
        };
        return Jobs.Count > 0;
    }

    private void OnSegmentSelected(SegmentItem segment)
    {
        foreach (var s in Segments)
            s.IsSelected = ReferenceEquals(s, segment);
        _filter = Enum.Parse<DriverJobFilter>(segment.Key);
        _ = LoadAsync(showSkeleton: true);
    }

    private Task OpenAsync(DriverJobSummary job) =>
        Navigation.GoToAsync(Routes.DriverJobDetail, Param(Routes.JobIdKey, job.Id));

    private Task AcceptAsync(DriverJobSummary job) => RunBusyAsync(async () =>
    {
        var route = job.IsPickup ? $"{job.FromName} ({job.FromArea}) → {job.ToName}" : $"{job.FromName} → {job.ToName} ({job.ToArea})";
        if (!await Dialogs.ConfirmAsync($"Accept {job.TypeText.ToLowerInvariant()} job?",
                $"{job.OrderNumber}\n{route}\nYou'll earn {CurrencyFormatter.Current.Format(job.Earnings)}.", "Accept", "Cancel"))
            return;

        await drivers.AcceptJobAsync(job.Id);
        await Navigation.GoToAsync(Routes.DriverJobDetail, Param(Routes.JobIdKey, job.Id));
    });

    [RelayCommand]
    private Task GoOnlineAsync() => RunBusyAsync(async () =>
    {
        await drivers.SetOnlineAsync(true);
        await LoadAsync(showSkeleton: true);
    });
}
