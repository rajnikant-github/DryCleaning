using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Shared base for pages that work on a single order (P04–P09).</summary>
public abstract partial class OrderPageViewModelBase(ViewModelServices services, IProviderOrderService orderService)
    : BaseViewModel(services), IQueryAttributable
{
    protected IProviderOrderService OrderService { get; } = orderService;
    protected Guid OrderId { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OrderNumber))]
    [NotifyPropertyChangedFor(nameof(CustomerName))]
    [NotifyPropertyChangedFor(nameof(Status))]
    [NotifyPropertyChangedFor(nameof(HeaderSubtitle))]
    private OrderDetail? detail;

    public string OrderNumber => Detail?.Order.OrderNumber ?? string.Empty;
    public string CustomerName => Detail?.CustomerName ?? string.Empty;
    public OrderStatus Status => Detail?.Order.Status ?? OrderStatus.Created;
    public string HeaderSubtitle => Detail is null ? string.Empty : $"{Detail.Order.OrderNumber} · {Detail.CustomerName}";

    public ObservableCollection<OrderItemDetail> Items { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.OrderIdKey, out var id))
            OrderId = id;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        Detail = await OrderService.GetOrderDetailAsync(OrderId);
        Items.Clear();
        foreach (var item in Detail.Items)
            Items.Add(item);

        OnDetailLoaded(Detail);
        return true;
    }

    protected virtual void OnDetailLoaded(OrderDetail detail)
    {
    }

    /// <summary>Navigates to the next workflow screen, replacing the current page.</summary>
    protected Task ReplaceWithAsync(string route) =>
        Navigation.GoToAsync($"../{route}", Param(Routes.OrderIdKey, OrderId));

    protected async Task<bool> ReportItemIssueAsync(string defaultReason)
    {
        var description = await Dialogs.PromptAsync(defaultReason, "Describe the issue. EDry Cleaners support and the customer will be notified.", "Report");
        if (string.IsNullOrWhiteSpace(description))
            return false;

        await OrderService.ReportIssueAsync(OrderId, defaultReason, description.Trim());
        await Dialogs.AlertAsync("Issue reported", "Thanks – we've logged this against the order.");
        return true;
    }
}
