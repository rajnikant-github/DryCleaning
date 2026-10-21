using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IDocumentPicker
{
    /// <summary>Lets the user take or choose a photo, or choose a PDF of at most <paramref name="maxPdfBytes"/>. Photos are shrunk (still readable) to fit <paramref name="maxImageBytes"/>. Null when they cancel.</summary>
    Task<PickedDocument?> PickAsync(long maxPdfBytes, long maxImageBytes);
}
