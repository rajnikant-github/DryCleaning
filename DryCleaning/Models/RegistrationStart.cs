namespace DryCleaning.Models;

/// <summary>The outcome of step 1 of sign-up: a code was emailed (and nothing is created yet), or why it couldn't be.</summary>
public sealed class RegistrationStart
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    /// <summary>The masked email the code went to, e.g. ra***@gmail.com.</summary>
    public string Destination { get; init; } = string.Empty;

    /// <summary>Seconds before "Resend code" can be used.</summary>
    public int ResendInSeconds { get; init; } = 60;

    public static RegistrationStart Ok(string destination, int resendInSeconds = 60) =>
        new() { Success = true, Destination = destination, ResendInSeconds = resendInSeconds };

    public static RegistrationStart Fail(string error) => new() { Success = false, Error = error };
}
