using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>P02 – Provider dashboard.</summary>
public sealed partial class DashboardViewModel(ViewModelServices services, IProviderOrderService orders)
    : BaseViewModel(services)
{
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.Reconnected || message.Kind == RealtimeChangeKind.OrderUpdated || message.Kind == RealtimeChangeKind.NotificationReceived;

    [ObservableProperty] private string greeting = string.Empty;
    [ObservableProperty] private string businessName = string.Empty;
    [ObservableProperty] private string todayText = string.Empty;
    [ObservableProperty] private int newCount;
    [ObservableProperty] private int inProcessCount;
    [ObservableProperty] private int readyCount;
    [ObservableProperty] private int completedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnread))]
    [NotifyPropertyChangedFor(nameof(UnreadText))]
    private int unreadCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIncomingDriver))]
    private OrderSummary? incomingDriverOrder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoTodaysOrders))]
    private bool hasTodaysOrders;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoRecentActivity))]
    private bool hasRecentActivity;

    public bool HasUnread => UnreadCount > 0;
    public string UnreadText => UnreadCount == 1 ? "1 new alert" : $"{UnreadCount} new alerts";
    public bool HasIncomingDriver => IncomingDriverOrder is not null;
    public bool HasNoTodaysOrders => !HasTodaysOrders;
    public bool HasNoRecentActivity => !HasRecentActivity;
    public string IncomingDriverName => IncomingDriverOrder?.DriverName ?? "Driver";
    public string IncomingDriverText => IncomingDriverOrder is null
        ? string.Empty
        : $"Bringing {IncomingDriverOrder.OrderNumber} · {IncomingDriverOrder.CustomerName} ({IncomingDriverOrder.ItemCount} items)";

    public ObservableCollection<OrderCardItem> TodaysOrders { get; } = [];
    public ObservableCollection<ActivityItem> RecentActivity { get; } = [];

    protected override async Task<bool> LoadDataAsync()
    {
        var dashboard = await orders.GetDashboardAsync();

        Greeting = $"{TimeFormat.Greeting(DateTime.Now)}, {dashboard.OwnerName.Split(' ')[0]}!";
        BusinessName = dashboard.BusinessName;
        TodayText = DateTime.Now.ToString("dddd, d MMMM");
        NewCount = dashboard.NewCount;
        InProcessCount = dashboard.InProcessCount;
        ReadyCount = dashboard.ReadyCount;
        CompletedCount = dashboard.CompletedCount;
        UnreadCount = dashboard.UnreadNotifications;
        IncomingDriverOrder = dashboard.IncomingDriverOrder;
        OnPropertyChanged(nameof(IncomingDriverName));
        OnPropertyChanged(nameof(IncomingDriverText));

        TodaysOrders.Clear();
        foreach (var order in dashboard.TodaysOrders)
            TodaysOrders.Add(new OrderCardItem(order, OpenOrderAsync, primaryAction: OpenNextStepAsync));
        HasTodaysOrders = TodaysOrders.Count > 0;

        RecentActivity.Clear();
        foreach (var entry in dashboard.RecentActivity)
            RecentActivity.Add(new ActivityItem(entry, id => Navigation.GoToAsync(Routes.OrderDetail, Param(Routes.OrderIdKey, id))));
        HasRecentActivity = RecentActivity.Count > 0;

        return true;
    }

    private Task OpenOrderAsync(OrderSummary order) =>
        Navigation.GoToAsync(Routes.OrderDetail, Param(Routes.OrderIdKey, order.Id));

    private Task OpenNextStepAsync(OrderSummary order)
    {
        var action = ProviderActions.ForStatus(order.Status);
        return action is null
            ? OpenOrderAsync(order)
            : Navigation.GoToAsync(action.Route, Param(Routes.OrderIdKey, order.Id));
    }

    [RelayCommand]
    private Task OpenFilterAsync(string filter) =>
        filter == nameof(ProviderOrderFilter.Completed)
            ? Navigation.GoToAsync(Routes.OrderHistory)
            : Navigation.GoToAsync(Routes.ProviderOrders, Param(Routes.FilterKey, filter));

    [RelayCommand]
    private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);

    [RelayCommand]
    private Task OpenIncomingAsync() =>
        IncomingDriverOrder is null
            ? Task.CompletedTask
            : Navigation.GoToAsync(Routes.ReceiveOrder, Param(Routes.OrderIdKey, IncomingDriverOrder.Id));

    [RelayCommand]
    private Task ViewAllOrdersAsync() => Navigation.GoToAsync(Routes.ProviderOrders);
}
