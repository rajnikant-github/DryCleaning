using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IPhotoService
{
    /// <summary>Lets the user take or choose a photo. Returns a local file path, or null if cancelled.</summary>
    Task<string?> AddPhotoAsync();

    /// <summary>
    /// Same, for a photo of a document: kept large and sharp enough to read small print (IDs, licences), and only shrunk further when the
    /// file would be bigger than <paramref name="maxBytes"/>.
    /// </summary>
    Task<string?> AddDocumentPhotoAsync(long maxBytes);
}
