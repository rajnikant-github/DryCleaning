namespace DryCleaning.Services.Push;

/// <summary>Tells the server which phone belongs to the signed-in user (POST/DELETE api/me/devices).</summary>
public interface IPushRegistry
{
    Task RegisterAsync(string token, string platform);
    Task UnregisterAsync(string token);
}
