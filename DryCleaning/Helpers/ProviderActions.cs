using DryCleaning.Models;

namespace DryCleaning.Helpers;

/// <summary>Maps an order status to the provider's next step screen.</summary>
public static class ProviderActions
{
    /// <param name="driverHandedOver">The provider can only receive a picked-up order after the driver has handed it over.</param>
    public static ProviderAction? ForStatus(OrderStatus status, bool driverHandedOver = true) => status switch
    {
        OrderStatus.PickedUp when !driverHandedOver => null,
        OrderStatus.PickedUp => new() { Label = "Receive Order", Route = Routes.ReceiveOrder },
        OrderStatus.ReceivedByProvider => new() { Label = "Inspect Items", Route = Routes.Inspection },
        OrderStatus.Processing => new() { Label = "Mark Quality Check", Route = Routes.Processing },
        OrderStatus.QualityCheck => new() { Label = "Complete Quality Check", Route = Routes.QualityCheck },
        OrderStatus.ReadyForDelivery => new() { Label = "Request Driver", Route = Routes.ReadyForDelivery },
        _ => null
    };

    public static string? HintFor(OrderStatus status, bool driverHandedOver = true) => status switch
    {
        OrderStatus.Confirmed => "Pickup job posted · waiting for a driver to accept",
        OrderStatus.PickupAssigned => "Driver assigned · collecting from customer",
        OrderStatus.PickedUp when !driverHandedOver => "Driver has the items · on the way to you",
        OrderStatus.PickedUp => "Driver handed over · confirm what arrived",
        OrderStatus.DeliveryAssigned => "Delivery driver assigned · collecting from you",
        OrderStatus.OutForDelivery => "Out for delivery to customer",
        _ => null
    };
}
