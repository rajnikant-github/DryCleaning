using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>Shared base for the screens that work on one driver job (D03–D12).</summary>
public abstract partial class DriverJobViewModelBase(ViewModelServices services, IDriverService drivers)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid? _orderId;

    protected IDriverService Drivers { get; } = drivers;
    protected Guid JobId { get; private set; }
    protected DriverJobDetail? Detail { get; private set; }

    [ObservableProperty] private string orderNumber = string.Empty;
    [ObservableProperty] private string typeIcon = string.Empty;
    [ObservableProperty] private string typeText = string.Empty;
    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private string providerName = string.Empty;
    [ObservableProperty] private string itemCountText = string.Empty;
    [ObservableProperty] private decimal earnings;
    [ObservableProperty] private DriverJobStep step;

    [ObservableProperty] private string destinationName = string.Empty;
    [ObservableProperty] private string destinationAddress = string.Empty;
    [ObservableProperty] private string destinationPhone = string.Empty;
    [ObservableProperty] private string distanceText = string.Empty;
    [ObservableProperty] private string etaText = string.Empty;

    [ObservableProperty] private bool hasDemoCode;
    [ObservableProperty] private string demoCodeText = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.JobIdKey, out var jobId))
            JobId = jobId;
        else if (TryGetGuid(query, Routes.OrderIdKey, out var orderId))
            _orderId = orderId;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        if (JobId == Guid.Empty && _orderId is { } orderId)
            JobId = await Drivers.FindJobForOrderAsync(orderId) ?? throw new InvalidOperationException("This job is no longer available.");

        var detail = await Drivers.GetJobAsync(JobId);
        Detail = detail;
        var job = detail.Job;

        OrderNumber = job.OrderNumber;
        TypeIcon = job.TypeIcon;
        TypeText = job.TypeText;
        CustomerName = detail.CustomerName;
        ProviderName = detail.ProviderName;
        ItemCountText = job.ItemCount == 1 ? "1 item" : $"{job.ItemCount} items";
        Earnings = job.Earnings;
        Step = detail.NextStep;

        DestinationName = detail.DestinationName;
        DestinationAddress = detail.Destination.FormattedAddress;
        DestinationPhone = detail.DestinationPhone;
        var km = job.DistanceKm;
        DistanceText = Geo.DistanceText(km);
        EtaText = $"~{Geo.EtaMinutes(km)} min";

        HasDemoCode = !string.IsNullOrEmpty(detail.DemoCustomerCode);
        DemoCodeText = $"Demo: the customer's code is {detail.DemoCustomerCode}";

        OnDetailLoaded(detail);
        return true;
    }

    protected virtual void OnDetailLoaded(DriverJobDetail detail)
    {
    }

    /// <summary>Screen the driver goes to after "I've arrived" for the current step.</summary>
    protected static string? ArrivalRoute(DriverJobStep step) => step switch
    {
        DriverJobStep.CollectFromCustomer => Routes.DriverVerifyItems,
        DriverJobStep.DropAtProvider => Routes.DriverHandover,
        DriverJobStep.CollectFromProvider => Routes.DriverCollect,
        DriverJobStep.DeliverToCustomer => Routes.DriverDeliver,
        _ => null
    };

    /// <summary>Replaces the current page with the next screen of this job.</summary>
    protected Task ReplaceWithAsync(string route, int pop = 1) =>
        Navigation.GoToAsync($"{string.Concat(Enumerable.Repeat("../", pop))}{route}", Param(Routes.JobIdKey, JobId));

    protected async Task CallAsync(string title, string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return;
        try
        {
            if (PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open(phone);
            else
                await Dialogs.AlertAsync(title, phone);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync(title, phone);
        }
    }

    [RelayCommand]
    private Task CallDestinationAsync() => CallAsync($"Call {DestinationName}", DestinationPhone);
}
