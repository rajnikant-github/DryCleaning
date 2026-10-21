namespace DryCleaning.Api.Domain;

/// <summary>
/// A sign-up waiting for its emailed code. No account exists until the code is confirmed; the row then becomes a real
/// account and is deleted. Only a hash of the code and of the password are stored.
/// </summary>
public class PendingRegistration : EntityBase
{
    public UserRole Role { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Role-specific fields (business name, categories, licence number) as JSON.</summary>
    public string DetailsJson { get; set; } = "{}";

    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    /// <summary>Wrong codes entered so far.</summary>
    public int Attempts { get; set; }

    /// <summary>How many codes have been emailed for this sign-up.</summary>
    public int SendCount { get; set; }

    public DateTime LastSentAt { get; set; } = DateTime.UtcNow;
}
