using Android.App;
using Android.Content;
using AndroidX.Core.App;
using DryCleaning.Services.Push;
using Firebase.Messaging;

namespace DryCleaning;

/// <summary>Receives Firebase push messages: new tokens go to the API, and messages that arrive while the app is open become notifications.</summary>
[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public class EdryFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        PushHub.TokenReceived(token);
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        // When the app is in the background Android shows the notification itself; this runs when the app is open.
        var notification = message.GetNotification();
        var data = message.Data;
        var title = notification?.Title ?? (data.TryGetValue("title", out var dataTitle) ? dataTitle : null) ?? "EDry Cleaners";
        var body = notification?.Body ?? (data.TryGetValue("body", out var dataBody) ? dataBody : null) ?? string.Empty;
        data.TryGetValue("orderId", out var orderId);

        var intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        if (orderId is not null)
            intent.PutExtra("orderId", orderId);

        var pending = PendingIntent.GetActivity(this, Guid.NewGuid().GetHashCode(), intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        var built = new NotificationCompat.Builder(this, PushChannel.Id)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(body))
            .SetAutoCancel(true)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetContentIntent(pending)
            .Build();

        NotificationManagerCompat.From(this).Notify(Guid.NewGuid().GetHashCode(), built);
    }
}
