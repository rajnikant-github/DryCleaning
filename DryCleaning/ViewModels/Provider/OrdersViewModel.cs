using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

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
