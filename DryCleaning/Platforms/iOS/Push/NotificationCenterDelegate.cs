using DryCleaning.Services.Push;
using Foundation;
using UserNotifications;

namespace DryCleaning;

/// <summary>Shows pushes as banners while the app is open and opens the order when a notification is tapped.</summary>
public sealed class NotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler) =>
        completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.List | UNNotificationPresentationOptions.Sound);

    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
    {
        var orderId = response.Notification.Request.Content.UserInfo["orderId"]?.ToString();
        PushHub.OrderTapped(orderId);
        completionHandler();
    }
}
