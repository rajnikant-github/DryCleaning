using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IProviderOrderService
{
    Task<ProviderDashboard> GetDashboardAsync();
    Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(ProviderOrderFilter filter);
    Task<IReadOnlyDictionary<ProviderOrderFilter, int>> GetOrderCountsAsync();
    Task<OrderDetail> GetOrderDetailAsync(Guid orderId);

    Task AcceptOrderAsync(Guid orderId);
    Task RejectOrderAsync(Guid orderId, string reason);
    Task ConfirmReceivedAsync(Guid orderId, ReceiveOrderRequest request);
    Task CompleteInspectionAsync(Guid orderId, IReadOnlyList<InspectionResult> results);
    Task MarkQualityCheckAsync(Guid orderId);
    Task SendBackToProcessingAsync(Guid orderId, string reason);
    Task MarkReadyForDeliveryAsync(Guid orderId, IReadOnlyList<string> photoPaths);
    /// <summary>Publishes a delivery job to the driver marketplace (a driver accepts it from the Driver app).</summary>
    Task RequestDeliveryDriverAsync(Guid orderId);

    /// <summary>Records a discrepancy/damage report without stopping the order.</summary>
    Task ReportIssueAsync(Guid orderId, string reason, string description);

    /// <summary>Raises a dispute and puts the order on hold (status Disputed).</summary>
    Task RaiseDisputeAsync(Guid orderId, string reason, string description);

    /// <summary>Mock-only: simulates the marketplace driver collecting items from the customer.</summary>
    Task SimulateDriverPickupAsync(Guid orderId);
}
