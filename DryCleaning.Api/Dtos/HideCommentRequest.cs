namespace DryCleaning.Api.Dtos;

public sealed class HideCommentRequest
{
    /// <summary>True hides the comment from the app; false shows it again.</summary>
    public bool Hidden { get; set; }
}
