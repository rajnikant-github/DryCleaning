namespace DryCleaning.Admin.Models;

public sealed class AdminCustomerDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int OrderCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
