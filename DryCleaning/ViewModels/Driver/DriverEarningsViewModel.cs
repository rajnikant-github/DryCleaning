using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D13 – Earnings for the last 7 days or 4 weeks.</summary>
public sealed partial class DriverEarningsViewModel(ViewModelServices services, IDriverService drivers)
    : BaseViewModel(services)
{
    private EarningsPeriod _period = EarningsPeriod.Week;

    public ObservableCollection<SegmentItem> Periods { get; } = [];
    public ObservableCollection<EarningsBarItem> Bars { get; } = [];
    public ObservableCollection<DriverJobCardItem> Jobs { get; } = [];

    [ObservableProperty] private decimal total;
    [ObservableProperty] private string periodText = string.Empty;
    [ObservableProperty] private string jobCountText = string.Empty;
    [ObservableProperty] private decimal averagePerJob;
    [ObservableProperty] private string splitText = string.Empty;
    [ObservableProperty] private string rateText = string.Empty;
    [ObservableProperty] private bool hasJobs;

    protected override async Task<bool> LoadDataAsync()
    {
        if (Periods.Count == 0)
        {
            Periods.Add(new SegmentItem(nameof(EarningsPeriod.Week), "This Week", OnPeriodSelected) { IsSelected = true });
            Periods.Add(new SegmentItem(nameof(EarningsPeriod.Month), "Last 4 Weeks", OnPeriodSelected));
        }

        var earnings = await drivers.GetEarningsAsync(_period);
        Total = earnings.Total;
        PeriodText = earnings.PeriodText;
        JobCountText = earnings.JobCount == 1 ? "1 job" : $"{earnings.JobCount} jobs";
        AveragePerJob = earnings.AveragePerJob;
        SplitText = $"📦 {earnings.PickupCount} pickups   🚚 {earnings.DeliveryCount} deliveries";
        RateText = $"You earn {CurrencyFormatter.Current.Format(earnings.RatePerJob)} per completed pickup or delivery.";

        var max = earnings.Bars.Count == 0 ? 0 : earnings.Bars.Max(b => b.Amount);
        Bars.Clear();
        foreach (var bar in earnings.Bars)
            Bars.Add(new EarningsBarItem(bar, max));

        Jobs.Clear();
        foreach (var job in earnings.Jobs)
            Jobs.Add(new DriverJobCardItem(job, j => Navigation.GoToAsync(Routes.DriverJobDetail, Param(Routes.JobIdKey, j.Id))));
        HasJobs = Jobs.Count > 0;
        return true;
    }

    private void OnPeriodSelected(SegmentItem item)
    {
        foreach (var p in Periods)
            p.IsSelected = ReferenceEquals(p, item);
        _period = Enum.Parse<EarningsPeriod>(item.Key);
        _ = LoadAsync(showSkeleton: false);
    }
}
