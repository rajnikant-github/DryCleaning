using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IPhotoService
{
    /// <summary>Lets the user take or choose a photo. Returns a local file path, or null if cancelled.</summary>
    Task<string?> AddPhotoAsync();
}
