using DryCleaning.Services.Push;
using Foundation;
using UIKit;
using UserNotifications;

namespace DryCleaning;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    private readonly NotificationCenterDelegate _notificationCenter = new();

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        // Must be set before the app finishes launching so a tap that starts the app is not lost.
        UNUserNotificationCenter.Current.Delegate = _notificationCenter;
        return base.FinishedLaunching(application, launchOptions);
    }

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        // APNs expects the token as lowercase hex.
        var token = Convert.ToHexString(deviceToken.ToArray()).ToLowerInvariant();
        System.Diagnostics.Debug.WriteLine($"Push token received ({token.Length} hex chars)");
        PushService.CompleteTokenRequest(token);
        PushHub.TokenReceived(token);
    }

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        System.Diagnostics.Debug.WriteLine($"Push registration failed: {error.LocalizedDescription}");
        PushService.CompleteTokenRequest(null);
    }
}
