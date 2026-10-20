using Microsoft.Extensions.Logging;

namespace DryCleaning.Services.Push;

/// <summary>
/// Shared push logic. The platform parts (permission prompt, push token) live in <c>Platforms/Android</c> and
/// <c>Platforms/iOS</c> as the partial method <see cref="RequestPlatformTokenAsync"/>.
/// </summary>
public sealed partial class PushService : IPushService
{
    private readonly IPushRegistry _registry;
    private readonly ISessionService _session;
    private readonly IOrderLinkService _orderLinks;
    private readonly ILogger<PushService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _token;          // latest token from the platform
    private string? _registeredToken; // token the server knows for the current user
    private Guid? _pendingOrder;      // notification tapped before the user was signed in

    public PushService(IPushRegistry registry, ISessionService session, IOrderLinkService orderLinks, ILogger<PushService> logger)
    {
        _registry = registry;
        _session = session;
        _orderLinks = orderLinks;
        _logger = logger;
        PushHub.Attach(OnTokenReceived, OnOrderTapped);
    }

    private static string PlatformName =>
#if IOS
        "iOS";
#else
        "Android";
#endif

    /// <summary>Platform code: asks for notification permission and returns the push token (null when denied or unavailable).</summary>
    private static partial Task<string?> RequestPlatformTokenAsync();

    public async Task EnableAsync()
    {
        try
        {
            var token = await RequestPlatformTokenAsync();
            if (!string.IsNullOrEmpty(token))
                _token = token;
            await RegisterAsync();
        }
        catch (Exception ex)
        {
            // Push is a nice-to-have: never block sign-in because of it.
            _logger.LogWarning(ex, "Could not enable push notifications");
        }

        await OpenPendingOrderAsync();
    }

    public async Task DisableAsync()
    {
        var token = _registeredToken ?? _token;
        if (string.IsNullOrEmpty(token))
            return;

        await _gate.WaitAsync();
        try
        {
            await _registry.UnregisterAsync(token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not unregister this phone from push notifications");
        }
        finally
        {
            _registeredToken = null;
            _gate.Release();
        }
    }

    private void OnTokenReceived(string token)
    {
        _token = token;
        if (_session.IsSignedIn)
            _ = RegisterAsync();
    }

    private void OnOrderTapped(Guid orderId)
    {
        if (!_session.IsSignedIn)
        {
            _pendingOrder = orderId;
            return;
        }

        _ = MainThread.InvokeOnMainThreadAsync(() => OpenOrderAsync(orderId));
    }

    private async Task RegisterAsync()
    {
        var token = _token;
        if (string.IsNullOrEmpty(token) || !_session.IsSignedIn || token == _registeredToken)
            return;

        await _gate.WaitAsync();
        try
        {
            await _registry.RegisterAsync(token, PlatformName);
            _registeredToken = token;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not register this phone for push notifications");
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task OpenPendingOrderAsync()
    {
        if (_pendingOrder is not { } orderId)
            return;

        _pendingOrder = null;
        // The sign-in flow is still moving to the home screen; let it finish before navigating on top of it.
        await Task.Delay(1500);
        await MainThread.InvokeOnMainThreadAsync(() => OpenOrderAsync(orderId));
    }

    private async Task OpenOrderAsync(Guid orderId)
    {
        try
        {
            await _orderLinks.OpenAsync(orderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not open order {OrderId} from a notification", orderId);
        }
    }
}
