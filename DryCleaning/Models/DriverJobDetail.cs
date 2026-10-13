namespace DryCleaning.Models;

public sealed class DriverJobDetail
{
    public required DriverJobSummary Job { get; init; }
    public required Address From { get; init; }
    public required Address To { get; init; }
    public string FromPhone { get; init; } = string.Empty;
    public string ToPhone { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public IReadOnlyList<OrderItemDetail> Items { get; init; } = [];
    public IReadOnlyList<OrderStatusHistory> History { get; init; } = [];
    public DriverJobStep NextStep { get; init; }

    /// <summary>True when this job belongs to the signed-in driver.</summary>
    public bool IsMine { get; init; }

    /// <summary>Mock/demo only (Features.ShowDemoActions): the customer's pickup code or delivery OTP, so the flow can be tested on one phone.</summary>
    public string? DemoCustomerCode { get; init; }

    /// <summary>Where the driver is heading for the next step: the job's start (From) or its drop-off (To).</summary>
    public bool HeadingToStart => NextStep is DriverJobStep.Accept or DriverJobStep.CollectFromCustomer or DriverJobStep.CollectFromProvider;

    public Address Destination => HeadingToStart ? From : To;
    public string DestinationName => HeadingToStart ? Job.FromName : Job.ToName;
    public string DestinationPhone => HeadingToStart ? FromPhone : ToPhone;
}
