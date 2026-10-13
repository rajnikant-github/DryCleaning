namespace DryCleaning.Models;

public sealed class OrderVerification : EntityBase
{
    public Guid OrderId { get; set; }
    public VerificationType Type { get; set; }
    public string? OtpCode { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }
}
