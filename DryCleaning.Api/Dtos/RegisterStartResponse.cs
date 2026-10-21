namespace DryCleaning.Api.Dtos;

public sealed class RegisterStartResponse
{
    /// <summary>The masked email the code was sent to, e.g. ra***@gmail.com.</summary>
    public required string Destination { get; init; }

    public required int ExpiresInSeconds { get; init; }

    /// <summary>How long the app should wait before offering "Resend code".</summary>
    public required int ResendInSeconds { get; init; }

}
