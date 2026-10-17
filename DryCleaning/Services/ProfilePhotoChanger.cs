namespace DryCleaning.Services;

/// <summary>
/// The "tap your avatar" flow shared by the customer, driver and provider profile pages:
/// choose / take a photo, upload it, or remove the current one.
/// </summary>
public sealed class ProfilePhotoChanger(IPhotoService photos, IUserPhotoService userPhotos, IDialogService dialogs)
{
    private const string Change = "Change photo";
    private const string Remove = "Remove photo";

    /// <summary>Returns whether the photo changed and the address to show it from (null when removed).</summary>
    public async Task<(bool Changed, string? Url)> RunAsync(bool hasPhoto)
    {
        if (hasPhoto)
        {
            var choice = await dialogs.ActionSheetAsync("Profile photo", "Cancel", Change, Remove);
            if (choice == Remove)
            {
                await userPhotos.RemoveAsync();
                return (true, null);
            }

            if (choice != Change)
                return (false, null);
        }

        var path = await photos.AddPhotoAsync();
        if (string.IsNullOrEmpty(path))
            return (false, null);

        return (true, await userPhotos.UploadAsync(path));
    }
}
