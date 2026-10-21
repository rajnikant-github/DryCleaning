namespace DryCleaning.Api.Infrastructure.Storage;

/// <summary>Who can open a stored file: anyone with the link, or only people holding a short-lived signed link.</summary>
public enum FileVisibility
{
    /// <summary>Provider logos – shown to every customer.</summary>
    Public,

    /// <summary>Order photos – only reachable through signed links the API hands out to the order's own parties.</summary>
    Private,

    /// <summary>
    /// Identity and business documents – never served as a static file and never given a link. The API reads them with
    /// <see cref="IFileStorage.OpenReadAsync"/> after checking who is asking.
    /// </summary>
    Protected
}
