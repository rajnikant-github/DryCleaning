using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>The driver's next step on a job (derived from job type + status).</summary>
public enum DriverJobStep
{
    /// <summary>Pending on the marketplace.</summary>
    Accept,
    /// <summary>Pickup · Assigned – go to the customer, verify items, enter the pickup code.</summary>
    CollectFromCustomer,
    /// <summary>Pickup · InProgress – take the items to the provider and hand over.</summary>
    DropAtProvider,
    /// <summary>Delivery · Assigned – collect the packed order from the provider.</summary>
    CollectFromProvider,
    /// <summary>Delivery · InProgress – deliver with the customer's OTP.</summary>
    DeliverToCustomer,
    Done,
    /// <summary>Cancelled, taken by another driver, or the order is on hold.</summary>
    Unavailable
}
