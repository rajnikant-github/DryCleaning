using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D02 – Jobs: Available (marketplace) / My Jobs / Completed.</summary>
public sealed partial class DriverJobsViewModel(ViewModelServices services, IDriverService drivers)
    : BaseViewModel(services), IQueryAttributable
{
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.Reconnected || message.Kind == RealtimeChangeKind.JobChanged;

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
        var route = job.IsPickup ? $"{job.FromName} ({job.FromArea}) to {job.ToName}" : $"{job.FromName} to {job.ToName} ({job.ToArea})";
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
