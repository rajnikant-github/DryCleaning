using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Push;

/// <summary>Sends pushes to one platform's devices (Android through Firebase, iOS straight through Apple).</summary>
public interface IPushSender
{
    DevicePlatform Platform { get; }

    /// <summary>Sends to the given device tokens. Returns tokens the platform reported as no longer valid.</summary>
    Task<IReadOnlyList<string>> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct);
}
