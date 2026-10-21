namespace DryCleaning.Admin.Models;

public sealed class DocumentEventDto
{
    public DocumentAction Action { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
