using DryCleaning.Models;

namespace DryCleaning.Helpers;

/// <summary>Maps an order status to the provider's next step screen.</summary>
public static class ProviderActions
{
    public static ProviderAction? ForStatus(OrderStatus status) => status switch
    {
        OrderStatus.PickedUp => new() { Label = "Receive Order", Route = Routes.ReceiveOrder },
        OrderStatus.ReceivedByProvider => new() { Label = "Inspect Items", Route = Routes.Inspection },
        OrderStatus.Processing => new() { Label = "Mark Quality Check", Route = Routes.Processing },
        OrderStatus.QualityCheck => new() { Label = "Complete Quality Check", Route = Routes.QualityCheck },
        OrderStatus.ReadyForDelivery => new() { Label = "Request Driver", Route = Routes.ReadyForDelivery },
        _ => null
    };

    public static string? HintFor(OrderStatus status) => status switch
    {
        OrderStatus.Confirmed => "Pickup job posted · waiting for a driver to accept",
        OrderStatus.PickupAssigned => "Driver assigned · collecting from customer",
        OrderStatus.PickedUp => "Driver is on the way to your store",
        OrderStatus.DeliveryAssigned => "Delivery driver assigned · collecting from you",
        OrderStatus.OutForDelivery => "Out for delivery to customer",
        _ => null
    };
}
