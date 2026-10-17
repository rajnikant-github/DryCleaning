namespace DryCleaning.Services;

/// <summary>The signed-in user's profile photo (customer, driver, or a provider's owner).</summary>
public interface IUserPhotoService
{
    /// <summary>Uploads a photo from a local file and returns the address to show it from.</summary>
    Task<string?> UploadAsync(string localPath);

    /// <summary>Removes the photo (back to the initials avatar).</summary>
    Task RemoveAsync();
}
