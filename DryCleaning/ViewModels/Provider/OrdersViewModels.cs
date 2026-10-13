using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Shared logic for segmented order lists (Orders tab and Order History).</summary>
public abstract partial class OrderListViewModelBase : BaseViewModel
{
    protected readonly IProviderOrderService OrderService;

    protected OrderListViewModelBase(ViewModelServices services, IProviderOrderService orderService, params (ProviderOrderFilter Filter, string Title)[] segments)
        : base(services)
    {
        OrderService = orderService;
        foreach (var (filter, title) in segments)
            Segments.Add(new SegmentItem(filter.ToString(), title, OnSegmentSelected));

        SelectedFilter = segments[0].Filter;
        Segments[0].IsSelected = true;
    }

    public ObservableCollection<SegmentItem> Segments { get; } = [];
    public ObservableCollection<OrderCardItem> Orders { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyTitle))]
    private ProviderOrderFilter selectedFilter;

    public string EmptyTitle => SelectedFilter switch
    {
        ProviderOrderFilter.New => "No new orders",
        ProviderOrderFilter.Incoming => "No incoming pickups",
        ProviderOrderFilter.InProcess => "Nothing in process",
        ProviderOrderFilter.Ready => "No orders ready",
        ProviderOrderFilter.Completed => "No completed orders",
        ProviderOrderFilter.Cancelled => "No cancelled orders",
        ProviderOrderFilter.Disputed => "No disputed orders",
        _ => "No orders"
    };

    protected void SelectFilter(ProviderOrderFilter filter)
    {
        SelectedFilter = filter;
        foreach (var segment in Segments)
            segment.IsSelected = segment.Key == filter.ToString();
    }

    private async void OnSegmentSelected(SegmentItem segment)
    {
        if (!Enum.TryParse<ProviderOrderFilter>(segment.Key, out var filter) || filter == SelectedFilter)
            return;

        SelectFilter(filter);
        await LoadAsync(showSkeleton: true);
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var counts = await OrderService.GetOrderCountsAsync();
        foreach (var segment in Segments)
            if (Enum.TryParse<ProviderOrderFilter>(segment.Key, out var f) && counts.TryGetValue(f, out var count))
                segment.Count = count;

        var list = await OrderService.GetOrdersAsync(SelectedFilter);
        Orders.Clear();
        foreach (var order in list)
            Orders.Add(CreateItem(order));

        return Orders.Count > 0;
    }

    protected virtual OrderCardItem CreateItem(OrderSummary order) => new(order, OpenOrderAsync);

    protected Task OpenOrderAsync(OrderSummary order) =>
        Navigation.GoToAsync(Routes.OrderDetail, Param(Routes.OrderIdKey, order.Id));
}

/// <summary>Orders tab – includes P03 New Orders (accept / reject).</summary>
public sealed partial class OrdersViewModel : OrderListViewModelBase, IQueryAttributable
{
    public OrdersViewModel(ViewModelServices services, IProviderOrderService orderService)
        : base(services, orderService,
            (ProviderOrderFilter.New, "New"),
            (ProviderOrderFilter.Incoming, "Incoming"),
            (ProviderOrderFilter.InProcess, "In Process"),
            (ProviderOrderFilter.Ready, "Ready"))
    {
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.FilterKey, out var value) &&
            Enum.TryParse<ProviderOrderFilter>(value?.ToString(), out var filter))
        {
            SelectFilter(filter);
        }
    }

    protected override OrderCardItem CreateItem(OrderSummary order) =>
        new(order, OpenOrderAsync, AcceptAsync, RejectAsync, OpenNextStepAsync);

    private Task OpenNextStepAsync(OrderSummary order)
    {
        var action = ProviderActions.ForStatus(order.Status);
        return action is null
            ? OpenOrderAsync(order)
            : Navigation.GoToAsync(action.Route, Param(Routes.OrderIdKey, order.Id));
    }

    private Task AcceptAsync(OrderSummary order) => RunBusyAsync(async () =>
    {
        await OrderService.AcceptOrderAsync(order.Id);
        await Dialogs.AlertAsync("Order accepted",
            $"{order.OrderNumber} is confirmed. A marketplace driver has been assigned to collect it from {order.CustomerName}.");
        await LoadAsync(showSkeleton: false);
    });

    private Task RejectAsync(OrderSummary order) => RunBusyAsync(async () =>
    {
        var reason = await Dialogs.ActionSheetAsync($"Reject {order.OrderNumber}?", "Cancel",
            "Fully booked today", "Service not available", "Outside service area", "Other");
        if (reason is null)
            return;

        if (reason == "Other")
        {
            reason = await Dialogs.PromptAsync("Reason for rejection", "Let the customer know why.", "Reject order");
            if (string.IsNullOrWhiteSpace(reason))
                return;
        }

        await OrderService.RejectOrderAsync(order.Id, reason);
        await LoadAsync(showSkeleton: false);
    });

    [RelayCommand]
    private Task OpenHistoryAsync() => Navigation.GoToAsync(Routes.OrderHistory);
}

/// <summary>P10 – Completed / Cancelled / Disputed orders.</summary>
public sealed class OrderHistoryViewModel(ViewModelServices services, IProviderOrderService orderService)
    : OrderListViewModelBase(services, orderService,
        (ProviderOrderFilter.Completed, "Completed"),
        (ProviderOrderFilter.Cancelled, "Cancelled"),
        (ProviderOrderFilter.Disputed, "Disputed"));
