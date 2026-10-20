using DryCleaning.Api.Domain;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace DryCleaning.Api.Infrastructure.Push;

/// <summary>Firebase Cloud Messaging for Android phones (iPhones are sent through <see cref="ApnsPushSender"/>).</summary>
public sealed class FcmPushSender : IPushSender
{
    private static readonly MessagingErrorCode[] DeadTokenErrors =
        [MessagingErrorCode.Unregistered, MessagingErrorCode.InvalidArgument, MessagingErrorCode.SenderIdMismatch];

    private readonly FirebaseMessaging _messaging;
    private readonly ILogger<FcmPushSender> _logger;

    public FcmPushSender(string credentialsPath, ILogger<FcmPushSender> logger)
    {
        _logger = logger;
        var app = FirebaseApp.GetInstance("edry") ?? FirebaseApp.Create(
            new AppOptions { Credential = GoogleCredential.FromFile(credentialsPath) }, "edry");
        _messaging = FirebaseMessaging.GetMessaging(app);
    }

    public DevicePlatform Platform => DevicePlatform.Android;

    public async Task<IReadOnlyList<string>> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct)
    {
        var data = new Dictionary<string, string>
        {
            ["notificationId"] = message.NotificationId.ToString(),
            ["type"] = message.Type
        };
        if (message.OrderId is { } orderId)
            data["orderId"] = orderId.ToString();

        var dead = new List<string>();
        foreach (var batch in tokens.Chunk(500)) // FCM multicast limit
        {
            var response = await _messaging.SendEachForMulticastAsync(new MulticastMessage
            {
                Tokens = batch,
                Notification = new FirebaseAdmin.Messaging.Notification { Title = message.Title, Body = message.Body },
                Data = data,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        ChannelId = PushChannels.Android,
                        // Without an event time Android shows a bogus age (e.g. "2032y") on notifications it displays itself.
                        EventTimestamp = DateTime.UtcNow
                    }
                }
            }, ct);

            for (var i = 0; i < response.Responses.Count; i++)
            {
                var result = response.Responses[i];
                if (result.IsSuccess)
                    continue;
                if (result.Exception?.MessagingErrorCode is { } code && DeadTokenErrors.Contains(code))
                    dead.Add(batch[i]);
                else
                    _logger.LogWarning(result.Exception, "Push to an Android device of user {UserId} failed", message.UserId);
            }
        }

        return dead;
    }
}
