namespace DryCleaning.Models;

// One class per table in the V1 schema. Kept as plain POCOs so they can be
// reused as API contracts when the mock services are replaced.

public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
