namespace DryCleaning.Services;

/// <summary>Opens the right order screen for the signed-in role (customer order, provider order or driver job).</summary>
public interface IOrderLinkService
{
    Task OpenAsync(Guid orderId);
}
