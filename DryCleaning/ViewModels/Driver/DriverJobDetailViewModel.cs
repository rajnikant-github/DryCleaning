using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D03 – Job details: route, items, earnings, Accept / Release / Continue.</summary>
public sealed partial class DriverJobDetailViewModel(ViewModelServices services, IDriverService drivers)
    : DriverJobViewModelBase(services, drivers)
{
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.Reconnected || (message.Kind == RealtimeChangeKind.JobChanged && message.JobId == JobId);

    public ObservableCollection<OrderItemDetail> Items { get; } = [];

    [ObservableProperty] private OrderStatus orderStatus;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private string stepTitle = string.Empty;
    [ObservableProperty] private string stepHint = string.Empty;

    [ObservableProperty] private string fromLabel = string.Empty;
    [ObservableProperty] private string fromName = string.Empty;
    [ObservableProperty] private string fromAddress = string.Empty;
    [ObservableProperty] private string fromPhone = string.Empty;
    [ObservableProperty] private string toLabel = string.Empty;
    [ObservableProperty] private string toName = string.Empty;
    [ObservableProperty] private string toAddress = string.Empty;
    [ObservableProperty] private string toPhone = string.Empty;
    [ObservableProperty] private string routeDistanceText = string.Empty;
    [ObservableProperty] private string slotText = string.Empty;
    [ObservableProperty] private bool hasSlot;
    [ObservableProperty] private bool showContacts;

    [ObservableProperty] private bool canAccept;
    [ObservableProperty] private bool canRelease;
    [ObservableProperty] private bool canContinue;
    [ObservableProperty] private string continueText = string.Empty;
    [ObservableProperty] private bool isDone;
    [ObservableProperty] private bool isUnavailable;
    [ObservableProperty] private bool isOnHold;
    [ObservableProperty] private string completedText = string.Empty;

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        var job = d.Job;
        Title = $"{job.TypeText} · {job.OrderNumber}";
        OrderStatus = job.OrderStatus;
        StatusText = DriverText.JobStatus(job.Status, job.JobType);
        StepTitle = DriverText.StepTitle(d.NextStep);
        StepHint = DriverText.StepHint(d.NextStep);

        FromLabel = job.IsPickup ? "Pick up from customer" : "Collect from provider";
        FromName = job.FromName;
        FromAddress = d.From.FormattedAddress;
        FromPhone = d.FromPhone;
        ToLabel = job.IsPickup ? "Drop off at provider" : "Deliver to customer";
        ToName = job.ToName;
        ToAddress = d.To.FormattedAddress;
        ToPhone = d.ToPhone;
        RouteDistanceText = $"{Geo.DistanceText(job.DistanceKm)} · ~{Geo.EtaMinutes(job.DistanceKm)} min drive";
        HasSlot = job.IsPickup && job.PickupDate is not null;
        SlotText = job.PickupDate is { } date ? $"{DriverText.Day(date)}{(string.IsNullOrEmpty(job.PickupSlot) ? "" : $" · {job.PickupSlot}")}" : string.Empty;

        // Phone numbers are only shared once the driver has the job.
        ShowContacts = d.IsMine && d.NextStep is not (DriverJobStep.Done or DriverJobStep.Unavailable);

        Items.Clear();
        foreach (var item in d.Items)
            Items.Add(item);

        IsOnHold = job.IsOnHold;
        CanAccept = d.NextStep == DriverJobStep.Accept;
        CanRelease = d.IsMine && job.Status == DriverJobStatus.Assigned && !job.IsOnHold;
        ContinueText = DriverText.ActionText(d.NextStep);
        CanContinue = !string.IsNullOrEmpty(ContinueText) && !job.IsOnHold;
        IsDone = d.NextStep == DriverJobStep.Done;
        IsUnavailable = d.NextStep == DriverJobStep.Unavailable || job.IsOnHold;
        CompletedText = job.CompletedAt is { } at
            ? $"Completed {at:d MMM, HH:mm} · {CurrencyFormatter.Current.Format(job.Earnings)} earned"
            : string.Empty;
    }

    [RelayCommand]
    private Task AcceptAsync() => RunBusyAsync(async () =>
    {
        await Drivers.AcceptJobAsync(JobId);
        await LoadAsync(showSkeleton: false);
    });

    [RelayCommand]
    private Task ReleaseAsync() => RunBusyAsync(async () =>
    {
        if (!await Dialogs.ConfirmAsync("Release job?", "The job goes back to the marketplace for another driver. Only release it if you can't do it.", "Release", "Keep job"))
            return;

        await Drivers.ReleaseJobAsync(JobId);
        await Navigation.GoBackAsync();
    });

    [RelayCommand]
    private Task ContinueAsync() => Navigation.GoToAsync(Routes.DriverNavigate, Param(Routes.JobIdKey, JobId));

    [RelayCommand]
    private Task CallFromAsync() => CallAsync($"Call {FromName}", FromPhone);

    [RelayCommand]
    private Task CallToAsync() => CallAsync($"Call {ToName}", ToPhone);
}
