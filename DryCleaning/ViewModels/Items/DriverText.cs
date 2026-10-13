using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

public static class DriverText
{
    public static string Day(DateTime date) =>
        date.Date == DateTime.Today ? "Today"
        : date.Date == DateTime.Today.AddDays(1) ? "Tomorrow"
        : date.Date == DateTime.Today.AddDays(-1) ? "Yesterday"
        : date.ToString("ddd d MMM");

    public static string JobStatus(DriverJobStatus status, DriverJobType type) => (status, type) switch
    {
        (DriverJobStatus.Pending, _) => "Available",
        (DriverJobStatus.Assigned, DriverJobType.Pickup) => "Go to customer",
        (DriverJobStatus.Assigned, DriverJobType.Delivery) => "Go to provider",
        (DriverJobStatus.InProgress, DriverJobType.Pickup) => "Items on board",
        (DriverJobStatus.InProgress, DriverJobType.Delivery) => "Out for delivery",
        (DriverJobStatus.Completed, _) => "Completed",
        _ => "Cancelled"
    };

    /// <summary>Short instruction for the next step.</summary>
    public static string StepTitle(DriverJobStep step) => step switch
    {
        DriverJobStep.Accept => "Accept this job",
        DriverJobStep.CollectFromCustomer => "Collect items from the customer",
        DriverJobStep.DropAtProvider => "Drop items at the provider",
        DriverJobStep.CollectFromProvider => "Collect the order from the provider",
        DriverJobStep.DeliverToCustomer => "Deliver to the customer",
        DriverJobStep.Done => "Job completed",
        _ => "Job unavailable"
    };

    public static string StepHint(DriverJobStep step) => step switch
    {
        DriverJobStep.Accept => "Accept to lock in this job. You can release it until you collect the items.",
        DriverJobStep.CollectFromCustomer => "Count the items with the customer, then enter the pickup code shown in their app.",
        DriverJobStep.DropAtProvider => "Hand the bag over at the store. The provider confirms receipt in their app.",
        DriverJobStep.CollectFromProvider => "Check the package count and order number before you leave the store.",
        DriverJobStep.DeliverToCustomer => "Ask the customer for the delivery OTP shown in their app.",
        DriverJobStep.Done => "Nice work – earnings for this job have been added.",
        _ => "This job was taken by another driver, cancelled or put on hold."
    };

    public static string ActionText(DriverJobStep step) => step switch
    {
        DriverJobStep.CollectFromCustomer => "Navigate to Customer",
        DriverJobStep.DropAtProvider => "Navigate to Provider",
        DriverJobStep.CollectFromProvider => "Navigate to Provider",
        DriverJobStep.DeliverToCustomer => "Navigate to Customer",
        _ => string.Empty
    };
}
