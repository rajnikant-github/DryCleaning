namespace DryCleaning.Models;

public sealed class OrderPhoto : EntityBase
{
    public Guid OrderId { get; set; }
    public PhotoType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public UploadedBy UploadedBy { get; set; }
}
