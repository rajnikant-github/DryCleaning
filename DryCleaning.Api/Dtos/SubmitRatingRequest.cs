namespace DryCleaning.Api.Dtos;

public sealed class SubmitRatingRequest
{
    /// <summary>1 to 5.</summary>
    public int Stars { get; set; }

    /// <summary>Optional comment (up to 500 characters).</summary>
    public string? Comment { get; set; }
}
