using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Shared logic for segmented order lists (Orders tab and Order History).</summary>
public abstract partial class OrderListViewModelBase : BaseViewModel
{
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.Reconnected || message.Kind == RealtimeChangeKind.OrderUpdated;

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
