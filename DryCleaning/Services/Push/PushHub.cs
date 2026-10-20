namespace DryCleaning.Services.Push;

/// <summary>
/// Entry point for the native push code (Android's FirebaseMessagingService, iOS's AppDelegate), which runs outside dependency
/// injection and possibly before the app has started. Events that arrive early are kept until <see cref="PushService"/> attaches.
/// </summary>
public static class PushHub
{
    private static readonly object Gate = new();
    private static Action<string>? _onToken;
    private static Action<Guid>? _onOrderTapped;
    private static string? _pendingToken;
    private static Guid? _pendingOrder;

    /// <summary>The platform produced a new push token (first run, or the platform rotated it).</summary>
    public static void TokenReceived(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return;

        Action<string>? handler;
        lock (Gate)
        {
            handler = _onToken;
            if (handler is null)
                _pendingToken = token;
        }

        handler?.Invoke(token);
    }

    /// <summary>The user tapped a notification that belongs to an order (orderId is the value from the push payload).</summary>
    public static void OrderTapped(string? orderId)
    {
        if (!Guid.TryParse(orderId, out var id))
            return;

        Action<Guid>? handler;
        lock (Gate)
        {
            handler = _onOrderTapped;
            if (handler is null)
                _pendingOrder = id;
        }

        handler?.Invoke(id);
    }

    internal static void Attach(Action<string> onToken, Action<Guid> onOrderTapped)
    {
        string? token;
        Guid? order;
        lock (Gate)
        {
            _onToken = onToken;
            _onOrderTapped = onOrderTapped;
            token = _pendingToken;
            order = _pendingOrder;
            _pendingToken = null;
            _pendingOrder = null;
        }

        if (token is not null)
            onToken(token);
        if (order is { } id)
            onOrderTapped(id);
    }
}
