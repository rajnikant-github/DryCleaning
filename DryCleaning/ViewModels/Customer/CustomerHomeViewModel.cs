using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C05 – Customer home.</summary>
public sealed partial class CustomerHomeViewModel(ViewModelServices services, ICustomerOrderService orders, ICartService cart)
    : CartAwareViewModel(services, cart)
{
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.Reconnected || message.Kind == RealtimeChangeKind.OrderUpdated;

    public ObservableCollection<CategoryCardItem> Categories { get; } = [];
    public ObservableCollection<OrderCardItem> RecentOrders { get; } = [];
    public ObservableCollection<PopularItemCard> PopularItems { get; } = [];

    [ObservableProperty] private string greeting = string.Empty;
    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string addressText = "Add your pickup address";
    [ObservableProperty] private bool hasUnread;
    [ObservableProperty] private OrderCardItem? activeOrder;
    [ObservableProperty] private bool hasActiveOrder;
    [ObservableProperty] private bool hasRecentOrders;

    protected override async Task<bool> LoadDataAsync()
    {
        var home = await orders.GetHomeAsync();
        Greeting = TimeFormat.Greeting(DateTime.Now) + ",";
        FirstName = home.FirstName;
        AddressText = home.Address?.FormattedAddress ?? "Add your pickup address";
        HasUnread = home.UnreadNotifications > 0;

        Categories.Clear();
        foreach (var category in home.Categories)
            Categories.Add(new CategoryCardItem(category, OpenCategoryAsync));

        ActiveOrder = home.ActiveOrder is null ? null : CreateOrderItem(home.ActiveOrder);
        HasActiveOrder = ActiveOrder is not null;

        RecentOrders.Clear();
        foreach (var order in home.RecentOrders)
            RecentOrders.Add(CreateOrderItem(order));
        HasRecentOrders = RecentOrders.Count > 0;

        PopularItems.Clear();
        foreach (var item in home.PopularItems)
            PopularItems.Add(new PopularItemCard(item, OpenPopularAsync));

        RefreshCart();
        return true;
    }

    private OrderCardItem CreateOrderItem(OrderSummary order) =>
        new(order, OpenOrderAsync, title: order.ProviderName, hint: order.Status.CustomerStatusMessage(order.IsPaid));

    private Task OpenOrderAsync(OrderSummary order) =>
        Navigation.GoToAsync(CustomerFlow.IsActive(order.Status) ? Routes.OrderTracking : Routes.CustomerOrderDetail,
            Param(Routes.OrderIdKey, order.Id));

    private Task OpenCategoryAsync(Category category) =>
        Navigation.GoToAsync(Routes.Providers, Param(Routes.CategoryIdKey, category.Id));

    private Task OpenPopularAsync(PopularServiceItem item) =>
        Navigation.GoToAsync(Routes.ItemDetails, new Dictionary<string, object>
        {
            [Routes.ServiceItemIdKey] = item.ServiceItemId,
            [Routes.ProductIdKey] = item.ProductId,
            [Routes.ProviderIdKey] = item.ProviderId,
            [Routes.ModeKey] = "quick"
        });

    [RelayCommand]
    private Task QuickOrderAsync() => Navigation.GoToAsync(Routes.Categories);

    [RelayCommand]
    private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);

    [RelayCommand]
    private Task ChangeAddressAsync() => Navigation.GoToAsync(Routes.PickupAddress, Param(Routes.ModeKey, "profile"));

    [RelayCommand]
    private Task ViewAllOrdersAsync() => Navigation.GoToAsync(Routes.CustomerOrders);
}
