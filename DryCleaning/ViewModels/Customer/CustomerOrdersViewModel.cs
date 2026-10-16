using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C17 – My orders (Active / Completed / Cancelled).</summary>
public sealed partial class CustomerOrdersViewModel(ViewModelServices services, ICustomerOrderService orders)
    : BaseViewModel(services)
{
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.Reconnected || message.Kind == RealtimeChangeKind.OrderUpdated;

    private CustomerOrderFilter _filter = CustomerOrderFilter.Active;

    public ObservableCollection<SegmentItem> Segments { get; } = [];
    public ObservableCollection<OrderCardItem> Orders { get; } = [];

    [ObservableProperty] private string emptyText = "No active orders";

    protected override async Task<bool> LoadDataAsync()
    {
        if (Segments.Count == 0)
        {
            foreach (var filter in Enum.GetValues<CustomerOrderFilter>())
                Segments.Add(new SegmentItem(filter.ToString(), filter.ToString(), OnSegmentSelected) { IsSelected = filter == _filter });
        }

        var counts = await orders.GetOrderCountsAsync();
        foreach (var segment in Segments)
            segment.Count = counts.TryGetValue(Enum.Parse<CustomerOrderFilter>(segment.Key), out var c) ? c : 0;

        var list = await orders.GetOrdersAsync(_filter);
        Orders.Clear();
        foreach (var order in list)
            Orders.Add(new OrderCardItem(order, OpenAsync, title: order.ProviderName,
                hint: order.Status.CustomerStatusMessage(order.IsPaid)));

        EmptyText = _filter switch
        {
            CustomerOrderFilter.Active => "No active orders",
            CustomerOrderFilter.Completed => "No completed orders yet",
            _ => "No cancelled orders"
        };
        return Orders.Count > 0;
    }

    private void OnSegmentSelected(SegmentItem segment)
    {
        foreach (var s in Segments)
            s.IsSelected = ReferenceEquals(s, segment);
        _filter = Enum.Parse<CustomerOrderFilter>(segment.Key);
        _ = LoadAsync(showSkeleton: true);
    }

    private Task OpenAsync(OrderSummary order) =>
        Navigation.GoToAsync(CustomerFlow.IsActive(order.Status) ? Routes.OrderTracking : Routes.CustomerOrderDetail,
            Param(Routes.OrderIdKey, order.Id));

    [RelayCommand]
    private Task NewOrderAsync() => Navigation.GoToAsync(Routes.Categories);
}
