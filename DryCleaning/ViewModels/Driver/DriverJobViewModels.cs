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

/// <summary>D03 – Job details: route, items, earnings, Accept / Release / Continue.</summary>
public sealed partial class DriverJobDetailViewModel(ViewModelServices services, IDriverService drivers)
    : DriverJobViewModelBase(services, drivers)
{
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

/// <summary>D04 / D10 – Navigate to the next stop, then "I've arrived".</summary>
public sealed partial class DriverNavigateViewModel(ViewModelServices services, IDriverService drivers)
    : DriverJobViewModelBase(services, drivers)
{
    [ObservableProperty] private string headingText = string.Empty;
    [ObservableProperty] private string destinationLabel = string.Empty;
    [ObservableProperty] private string arrivedText = "I've Arrived";
    [ObservableProperty] private string stepHint = string.Empty;

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        var toCustomer = d.NextStep is DriverJobStep.CollectFromCustomer or DriverJobStep.DeliverToCustomer;
        HeadingText = toCustomer ? "Heading to the customer" : "Heading to the provider";
        DestinationLabel = d.NextStep switch
        {
            DriverJobStep.CollectFromCustomer => "Collect from",
            DriverJobStep.DropAtProvider => "Drop off at",
            DriverJobStep.CollectFromProvider => "Collect from",
            _ => "Deliver to"
        };
        Title = toCustomer ? "Navigate to Customer" : "Navigate to Provider";
        StepHint = DriverText.StepHint(d.NextStep);
    }

    [RelayCommand]
    private async Task OpenMapsAsync()
    {
        if (Detail is null)
            return;

        var destination = Detail.Destination;
        try
        {
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(
                new Microsoft.Maui.Devices.Sensors.Location((double)destination.Latitude, (double)destination.Longitude),
                new Microsoft.Maui.ApplicationModel.MapLaunchOptions { Name = DestinationName, NavigationMode = Microsoft.Maui.ApplicationModel.NavigationMode.Driving });
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Maps", $"Couldn't open a maps app. Address:\n{DestinationAddress}");
        }
    }

    [RelayCommand]
    private Task ArrivedAsync()
    {
        var route = ArrivalRoute(Step);
        return route is null ? Navigation.GoBackAsync() : ReplaceWithAsync(route);
    }
}

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

/// <summary>D07 – Pickup complete → navigate to the provider.</summary>
public sealed partial class DriverPickupCompleteViewModel(ViewModelServices services, IDriverService drivers)
    : DriverJobViewModelBase(services, drivers)
{
    protected override void OnDetailLoaded(DriverJobDetail d) => Title = "Pickup Complete";

    [RelayCommand]
    private Task NavigateToProviderAsync() => ReplaceWithAsync(Routes.DriverNavigate);
}

/// <summary>D08 – Hand the items over to the provider (pickup job completed).</summary>
public sealed partial class DriverHandoverViewModel(ViewModelServices services, IDriverService drivers, IPhotoService photos)
    : DriverJobViewModelBase(services, drivers)
{
    public ObservableCollection<OrderItemDetail> Items { get; } = [];
    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty] private string notes = string.Empty;

    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        Title = "Handover to Provider";
        Items.Clear();
        foreach (var item in d.Items)
            Items.Add(item);
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await photos.AddPhotoAsync();
        if (path is not null)
            Photos.Add(path);
    }

    [RelayCommand]
    private Task ConfirmAsync() => RunBusyAsync(async () =>
    {
        if (!await Dialogs.ConfirmAsync("Confirm handover",
                $"Confirm you've handed {ItemCountText} for {OrderNumber} to {ProviderName}?", "Confirm", "Cancel"))
            return;

        await Drivers.ConfirmHandoverAsync(JobId, string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(), Photos.ToList());
        await ReplaceWithAsync(Routes.DriverJobCompleted);
    });
}

/// <summary>D09 – Collect the packed order from the provider → OutForDelivery.</summary>
public sealed partial class DriverCollectViewModel(ViewModelServices services, IDriverService drivers, IPhotoService photos)
    : DriverJobViewModelBase(services, drivers)
{
    public ObservableCollection<ChecklistItem> Checklist { get; } = [];
    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty] private int packageCount = 1;

    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        Title = "Collect from Provider";
        Checklist.Clear();
        Checklist.Add(new ChecklistItem($"Order number on the bag is {d.Job.OrderNumber}", OnChecklistChanged));
        Checklist.Add(new ChecklistItem($"Package holds {ItemCountText} ({d.Job.ItemsText})", OnChecklistChanged));
        Checklist.Add(new ChecklistItem("Package is sealed and undamaged", OnChecklistChanged));
        StartDeliveryCommand.NotifyCanExecuteChanged();
    }

    private void OnChecklistChanged() => StartDeliveryCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await photos.AddPhotoAsync();
        if (path is not null)
            Photos.Add(path);
    }

    private bool CanStart() => Checklist.Count > 0 && Checklist.All(c => c.IsChecked);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task StartDeliveryAsync() => RunBusyAsync(async () =>
    {
        await Drivers.StartDeliveryAsync(JobId, PackageCount, Photos.ToList());
        await ReplaceWithAsync(Routes.DriverNavigate);
    });

    [RelayCommand]
    private Task ReportProblemAsync() => Dialogs.AlertAsync("Problem at the store",
        "Don't take a package that is damaged or doesn't match. Ask the provider to fix it, or call EDry Cleaners support on +27 21 555 0100.");
}

/// <summary>D11 – Deliver with the customer's OTP → Delivered → Completed.</summary>
public sealed partial class DriverDeliverViewModel(ViewModelServices services, IDriverService drivers, IPhotoService photos)
    : DriverJobViewModelBase(services, drivers)
{
    public const int CodeLength = 4;

    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string code = string.Empty;

    [ObservableProperty] private bool hasCodeError;
    [ObservableProperty] private string codeError = string.Empty;

    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    protected override void OnDetailLoaded(DriverJobDetail d) => Title = "Deliver Order";

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

        try
        {
            IsBusy = true;
            await Drivers.ConfirmDeliveryAsync(JobId, Code, Photos.ToList());
            await ReplaceWithAsync(Routes.DriverJobCompleted);
        }
        catch (InvalidOperationException ex)
        {
            CodeError = ex.Message;
            HasCodeError = true;
            Code = string.Empty;
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync("Couldn't confirm delivery", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CustomerUnavailableAsync()
    {
        var call = await Dialogs.ConfirmAsync("Customer not available",
            "Call the customer first. If you can't reach them after 10 minutes, contact support on +27 21 555 0100 – don't leave the order unattended.",
            "Call customer", "Close");
        if (call)
            await CallAsync($"Call {CustomerName}", DestinationPhone);
    }
}

/// <summary>D12 – Job completed with earnings.</summary>
public sealed partial class DriverJobCompletedViewModel(ViewModelServices services, IDriverService drivers)
    : DriverJobViewModelBase(services, drivers)
{
    [ObservableProperty] private string headline = string.Empty;
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private string routeText = string.Empty;
    [ObservableProperty] private string completedText = string.Empty;
    [ObservableProperty] private decimal earningsToday;
    [ObservableProperty] private int completedToday;

    protected override async Task<bool> LoadDataAsync()
    {
        var loaded = await base.LoadDataAsync();
        var dashboard = await Drivers.GetDashboardAsync();
        EarningsToday = dashboard.EarningsToday;
        CompletedToday = dashboard.CompletedToday;
        return loaded;
    }

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        Title = "Job Completed";
        Headline = d.Job.IsPickup ? "Pickup job completed!" : "Delivery completed!";
        Message = d.Job.IsPickup
            ? $"{d.ProviderName} will confirm receipt and start cleaning."
            : $"{d.CustomerName} has their order. Thanks for delivering with EDry Cleaners!";
        RouteText = $"{d.Job.FromName} → {d.Job.ToName}";
        CompletedText = d.Job.CompletedAt is { } at ? at.ToString("d MMM yyyy, HH:mm") : string.Empty;
    }

    [RelayCommand]
    private Task FindMoreJobsAsync() => Navigation.GoToAsync(Routes.DriverJobs, Param(Routes.FilterKey, nameof(DriverJobFilter.Available)));

    [RelayCommand]
    private Task GoToDashboardAsync() => Navigation.GoToAsync(Routes.DriverDashboard);
}
