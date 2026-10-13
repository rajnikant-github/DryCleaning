namespace DryCleaning.Api.Domain;

/// <summary>A business rule was violated (mapped to HTTP 409).</summary>
public class DomainException(string message) : Exception(message);

public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void Touch() => UpdatedAt = DateTime.UtcNow;
}
