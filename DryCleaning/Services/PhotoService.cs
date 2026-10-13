using DryCleaning.Models;

namespace DryCleaning.Services;

public sealed class PhotoService(IDialogService dialogs) : IPhotoService
{
    private const string TakePhoto = "Take photo";
    private const string ChooseFromGallery = "Choose from gallery";

    public async Task<string?> AddPhotoAsync()
    {
        try
        {
            var options = MediaPicker.Default.IsCaptureSupported
                ? new[] { TakePhoto, ChooseFromGallery }
                : new[] { ChooseFromGallery };

            var choice = await dialogs.ActionSheetAsync("Add photo", "Cancel", options);
            if (choice is null)
                return null;

            FileResult? result;
            if (choice == TakePhoto)
            {
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await dialogs.AlertAsync("Camera permission", "Please allow camera access in Settings to take photos.");
                    return null;
                }

                result = await MediaPicker.Default.CapturePhotoAsync();
            }
            else
            {
                result = await MediaPicker.Default.PickPhotoAsync();
            }

            if (result is null)
                return null;

            var folder = Path.Combine(FileSystem.CacheDirectory, "order-photos");
            Directory.CreateDirectory(folder);
            var extension = Path.GetExtension(result.FileName);
            var target = Path.Combine(folder, $"{Guid.NewGuid():N}{(string.IsNullOrEmpty(extension) ? ".jpg" : extension)}");

            await using var source = await result.OpenReadAsync();
            await using var destination = File.Create(target);
            await source.CopyToAsync(destination);
            return target;
        }
        catch (FeatureNotSupportedException)
        {
            await dialogs.AlertAsync("Not supported", "Photos aren't supported on this device.");
            return null;
        }
        catch (PermissionException)
        {
            await dialogs.AlertAsync("Permission needed", "Please allow photo access in Settings.");
            return null;
        }
    }
}
