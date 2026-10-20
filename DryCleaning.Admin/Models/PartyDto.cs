namespace DryCleaning.Admin.Models;

public sealed class PartyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
