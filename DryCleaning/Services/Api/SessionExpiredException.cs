namespace DryCleaning.Services.Api;

/// <summary>The refresh token is no longer valid – the user has to sign in again.</summary>
public sealed class SessionExpiredException() : Exception("Your session has expired. Please sign in again.");
