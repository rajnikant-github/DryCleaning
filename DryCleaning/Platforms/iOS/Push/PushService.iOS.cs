using UIKit;
using UserNotifications;

namespace DryCleaning.Services.Push;

public sealed partial class PushService
{
    /// <summary>Completed by AppDelegate when iOS hands over the device token (or reports a failure).</summary>
    private static TaskCompletionSource<string?>? _tokenRequest;

    internal static void CompleteTokenRequest(string? token) => _tokenRequest?.TrySetResult(token);

    private static async partial Task<string?> RequestPlatformTokenAsync()
    {
        var (granted, _) = await UNUserNotificationCenter.Current.RequestAuthorizationAsync(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Badge);
        if (!granted)
            return null;

        var request = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _tokenRequest = request;
        await MainThread.InvokeOnMainThreadAsync(() => UIApplication.SharedApplication.RegisterForRemoteNotifications());

        // The token arrives asynchronously in AppDelegate; don't wait forever (e.g. no internet or no push entitlement).
        var finished = await Task.WhenAny(request.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        return finished == request.Task ? await request.Task : null;
    }
}
