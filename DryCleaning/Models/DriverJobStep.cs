namespace DryCleaning.Models;

/// <summary>The next thing the driver has to do on a job (derived from job type + status).</summary>
public enum DriverJobStep
{
    Accept,               // Pending on the marketplace
    CollectFromCustomer,  // Pickup · Assigned      → go to customer, verify items, pickup code
    DropAtProvider,       // Pickup · InProgress    → go to provider, hand over
    CollectFromProvider,  // Delivery · Assigned    → go to provider, collect package
    DeliverToCustomer,    // Delivery · InProgress  → go to customer, delivery OTP
    Done,                 // Completed
    Unavailable           // Cancelled, taken by someone else or order on hold
}
