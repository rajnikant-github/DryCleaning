using Android.Gms.Extensions;
using Firebase.Messaging;

namespace DryCleaning.Services.Push;

public sealed partial class PushService
{
    private static async partial Task<string?> RequestPlatformTokenAsync()
    {
        // Android 13+ needs the notification permission at runtime; older versions allow notifications by default.
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var status = await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<Permissions.PostNotifications>());
            if (status != PermissionStatus.Granted)
                return null;
        }

        var token = await FirebaseMessaging.Instance.GetToken().AsAsync<Java.Lang.String>();
        return token?.ToString();
    }
}
