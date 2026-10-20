namespace DryCleaning.Services.Push;

/// <summary>Push notifications on this phone: asks permission, registers the phone with the API and opens tapped notifications.</summary>
public interface IPushService
{
    /// <summary>Call after sign-in: asks for permission (first time), gets the push token and registers it with the API.</summary>
    Task EnableAsync();

    /// <summary>Call before sign-out, while the session is still valid: stops pushes to this phone for the signed-in user.</summary>
    Task DisableAsync();
}
