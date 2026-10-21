using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;

namespace DryCleaning.ViewModels;

/// <summary>Full-screen announcement shown when a paid order or a driver job arrives in real time.</summary>
public sealed partial class IncomingOrderAlertViewModel(ViewModelServices services, IOrderLinkService orderLinks)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid _orderId;

    [ObservableProperty] private string heading = "New order";
    [ObservableProperty] private string instruction = "Tap the screen to review the order";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.OrderIdKey, out var orderValue))
            Guid.TryParse(orderValue?.ToString(), out _orderId);

        if (!query.TryGetValue(Routes.JobTypeKey, out var jobTypeValue))
        {
            Heading = "New order";
            Instruction = "Tap the screen to review the order";
            return;
        }

        Heading = string.Equals(jobTypeValue?.ToString(), "Delivery", StringComparison.OrdinalIgnoreCase)
            ? "New delivery job"
            : "New pickup job";
        Instruction = "Tap the screen to review the job";
    }

    [RelayCommand]
    private Task ReviewAsync() => _orderId == Guid.Empty
        ? Task.CompletedTask
        : orderLinks.OpenAsync(_orderId);
}
