namespace DryCleaning.Api.Domain;

public class OrderPhoto : EntityBase
{
    public Guid OrderId { get; set; }
    public PhotoType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public UploadedBy UploadedBy { get; set; }

    public Order Order { get; set; } = null!;
}
