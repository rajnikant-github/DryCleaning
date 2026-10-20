namespace DryCleaning.Admin.Models;

public sealed class OrderPhotoDto
{
    public Guid Id { get; set; }
    public PhotoType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public UploadedBy UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
