namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/customer/orders/{id}/rating.</summary>
public sealed class SubmitRatingRequest
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
}
