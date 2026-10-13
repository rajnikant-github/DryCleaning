namespace DryCleaning.Api.Domain;

/// <summary>
/// Long-lived token used to get a new access token. Only a SHA-256 hash is stored.
/// Tokens rotate on every use; re-using a rotated token revokes all of the user's sessions.
/// </summary>
public class RefreshToken : EntityBase
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
