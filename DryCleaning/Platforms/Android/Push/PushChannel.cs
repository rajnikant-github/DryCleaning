using Android.App;
using Android.Content;
using Android.OS;

namespace DryCleaning;

/// <summary>The Android notification channel for order updates (must match the channel id the API sends).</summary>
public static class PushChannel
{
    public const string Id = "edry_orders";

    public static void Create(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            return;

        var channel = new NotificationChannel(Id, "Order updates", NotificationImportance.High)
        {
            Description = "Order status changes, driver updates and payments"
        };
        if (context.GetSystemService(Context.NotificationService) is NotificationManager manager)
            manager.CreateNotificationChannel(channel);
    }
}
