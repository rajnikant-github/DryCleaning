namespace DryCleaning.Configuration;

/// <summary>A demo account the Login screen fills in (Debug builds only).</summary>
public sealed class DebugLogin
{
    public string Identifier { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public bool IsComplete => !string.IsNullOrWhiteSpace(Identifier) && !string.IsNullOrEmpty(Password);
}
