using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

// Item ViewModels for the Driver screens (each carries its own commands).

public sealed class DriverJobCardItem
{
    public DriverJobCardItem(DriverJobSummary job, Func<DriverJobSummary, Task> open, Func<DriverJobSummary, Task>? accept = null)
    {
        Job = job;
        OpenCommand = new AsyncRelayCommand(() => open(job));
        AcceptCommand = new AsyncRelayCommand(() => accept?.Invoke(job) ?? Task.CompletedTask);
        ShowAccept = accept is not null && job.Status == DriverJobStatus.Pending;
    }

    public DriverJobSummary Job { get; }

    public string TypeIcon => Job.TypeIcon;
    public string TypeText => Job.TypeText;
    public bool IsPickup => Job.IsPickup;
    public string OrderNumber => Job.OrderNumber;
    public string FromName => Job.FromName;
    public string FromArea => Job.FromArea;
    public string ToName => Job.ToName;
    public string ToArea => Job.ToArea;
    public string ItemsText => Job.ItemCount == 1 ? "1 item" : $"{Job.ItemCount} items";
    public decimal Earnings => Job.Earnings;
    public string DistanceText => Geo.DistanceText(Job.DistanceKm);

    public string WhenText => Job.Status switch
    {
        DriverJobStatus.Completed when Job.CompletedAt is { } at => $"Completed {TimeFormat.Relative(at)}",
        DriverJobStatus.Cancelled => "Cancelled",
        _ when Job.IsPickup && Job.PickupDate is { } date =>
            $"{DriverText.Day(date)}{(string.IsNullOrEmpty(Job.PickupSlot) ? "" : $" · {Job.PickupSlot}")}",
        _ => $"Posted {TimeFormat.Relative(Job.CreatedAt)}"
    };

    public string StatusText => DriverText.JobStatus(Job.Status, Job.JobType);
    public bool ShowStatus => Job.Status != DriverJobStatus.Pending;
    public bool IsOnHold => Job.IsOnHold;
    public bool ShowAccept { get; }

    public IAsyncRelayCommand OpenCommand { get; }
    public IAsyncRelayCommand AcceptCommand { get; }
}
