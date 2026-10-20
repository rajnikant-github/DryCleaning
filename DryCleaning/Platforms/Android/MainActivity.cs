using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using DryCleaning.Services.Push;

namespace DryCleaning;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleNotificationTap(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleNotificationTap(intent);
    }

    /// <summary>A tapped push notification brings its data keys as intent extras (orderId).</summary>
    private static void HandleNotificationTap(Intent? intent)
    {
        var orderId = intent?.GetStringExtra("orderId");
        if (!string.IsNullOrEmpty(orderId))
        {
            intent!.RemoveExtra("orderId"); // handle once, not again after a rotation
            PushHub.OrderTapped(orderId);
        }
    }
}
