namespace DryCleaning.Admin.Models;

public sealed class AdminProviderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string BusinessName { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = [];
    public int OrderCount { get; set; }
    public bool IsActive { get; set; }
    public ProfileStatus ProfileStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
