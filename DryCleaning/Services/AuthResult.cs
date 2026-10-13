using DryCleaning.Models;

namespace DryCleaning.Services;

public sealed class AuthResult
{
    public required bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>Account created but not signed in yet – a Service Provider waiting for admin approval.</summary>
    public bool PendingApproval { get; init; }

    /// <summary>Shown to the user when <see cref="PendingApproval"/> is true.</summary>
    public string? Message { get; init; }

    public static AuthResult Ok() => new() { Success = true };
    public static AuthResult Fail(string error) => new() { Success = false, Error = error };
    public static AuthResult Pending(string message) => new() { Success = true, PendingApproval = true, Message = message };
}
