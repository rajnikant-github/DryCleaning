using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services;

public sealed class OrderLinkService(INavigationService navigation, ISessionService session, IDriverService drivers, IDialogService dialogs) : IOrderLinkService
{
    public async Task OpenAsync(Guid orderId)
    {
        switch (session.CurrentUser?.Role)
        {
            case UserRole.Customer:
                await navigation.GoToAsync(Routes.CustomerOrderDetail, Param(Routes.OrderIdKey, orderId));
                break;
            case UserRole.Driver:
                if (await drivers.FindJobForOrderAsync(orderId) is { } jobId)
                    await navigation.GoToAsync(Routes.DriverJobDetail, Param(Routes.JobIdKey, jobId));
                else
                    await dialogs.AlertAsync("Job unavailable", "This job is no longer available.");
                break;
            default:
                await navigation.GoToAsync(Routes.OrderDetail, Param(Routes.OrderIdKey, orderId));
                break;
        }
    }

    private static Dictionary<string, object> Param(string key, object value) => new() { [key] = value };
}
