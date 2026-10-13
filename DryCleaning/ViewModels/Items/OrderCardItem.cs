using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

// Item ViewModels carry their own commands so DataTemplates can use compiled
// bindings without RelativeSource lookups back to the page ViewModel.

public sealed class OrderCardItem
{
    public OrderCardItem(
        OrderSummary summary,
        Func<OrderSummary, Task> open,
        Func<OrderSummary, Task>? accept = null,
        Func<OrderSummary, Task>? reject = null,
        Func<OrderSummary, Task>? primaryAction = null,
        string? title = null,
        string? hint = null)
    {
        Summary = summary;
        Title = title ?? summary.CustomerName;
        OpenCommand = new AsyncRelayCommand(() => open(summary));
        AcceptCommand = new AsyncRelayCommand(() => accept?.Invoke(summary) ?? Task.CompletedTask);
        RejectCommand = new AsyncRelayCommand(() => reject?.Invoke(summary) ?? Task.CompletedTask);
        PrimaryActionCommand = new AsyncRelayCommand(() => primaryAction?.Invoke(summary) ?? Task.CompletedTask);

        ShowAcceptReject = accept is not null && summary.Status == OrderStatus.Created;
        var action = ProviderActions.ForStatus(summary.Status);
        HasPrimaryAction = !ShowAcceptReject && primaryAction is not null && action is not null;
        PrimaryActionText = action?.Label ?? string.Empty;
        HintText = hint ?? (summary.HasOpenDispute && summary.Status != OrderStatus.Disputed
            ? "⚠ Issue reported"
            : ProviderActions.HintFor(summary.Status) ?? string.Empty);
    }

    public OrderSummary Summary { get; }

    public string OrderNumber => Summary.OrderNumber;
    public string CustomerName => Summary.CustomerName;

    /// <summary>Card heading: customer name (provider app) or provider name (customer app).</summary>
    public string Title { get; }
    public string Subtitle => $"{Summary.OrderNumber} · {Summary.ItemsText}";
    public string ItemCountText => Summary.ItemCount == 1 ? "1 item" : $"{Summary.ItemCount} items";
    public decimal TotalAmount => Summary.TotalAmount;
    public OrderStatus Status => Summary.Status;
    public string TimeText => TimeFormat.Relative(Summary.UpdatedAt);

    public string HintText { get; }
    public bool HasHint => !string.IsNullOrEmpty(HintText);
    public bool ShowAcceptReject { get; }
    public bool HasPrimaryAction { get; }
    public string PrimaryActionText { get; }
    public bool HasActions => ShowAcceptReject || HasPrimaryAction;

    public IAsyncRelayCommand OpenCommand { get; }
    public IAsyncRelayCommand AcceptCommand { get; }
    public IAsyncRelayCommand RejectCommand { get; }
    public IAsyncRelayCommand PrimaryActionCommand { get; }
}
