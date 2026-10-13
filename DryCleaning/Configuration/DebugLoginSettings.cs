using DryCleaning.Models;

namespace DryCleaning.Configuration;

/// <summary>
/// Demo accounts per role, pre-filled on the Login screen in Debug builds (see environments/local.json and mock.json).
/// Release builds never read these. Leave a role out to start with empty fields.
/// </summary>
public sealed class DebugLoginSettings
{
    public DebugLogin? Customer { get; set; }
    public DebugLogin? Provider { get; set; }
    public DebugLogin? Driver { get; set; }

    public DebugLogin? For(UserRole role)
    {
        var login = role switch
        {
            UserRole.Customer => Customer,
            UserRole.Provider => Provider,
            UserRole.Driver => Driver,
            _ => null
        };
        return login is { IsComplete: true } ? login : null;
    }
}
