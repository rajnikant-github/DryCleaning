using DryCleaning.Models;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace DryCleaning.Services;

public sealed class PhotoService(IDialogService dialogs) : IPhotoService
{
    private const string TakePhoto = "Take photo";
    private const string ChooseFromGallery = "Choose from gallery";

    // Phone photos are often 5–10 MB; the API rejects anything over 5 MB and uploads are slow on mobile data.
    private const float MaxLongestSide = 1600f;
    private const float JpegQuality = 0.8f;
    private const long SkipResizeBelowBytes = 500 * 1024;

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
            destination.Close();

            return await Task.Run(() => Shrink(target));
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

    /// <summary>
    /// Scales the photo down to <see cref="MaxLongestSide"/> pixels and saves it as JPEG. Falls back to the original
    /// file if the format can't be decoded, so a photo is never lost because of resizing.
    /// </summary>
    private static string Shrink(string path)
    {
        try
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            if (new FileInfo(path).Length < SkipResizeBelowBytes && extension is ".jpg" or ".jpeg")
                return path;

            using var input = File.OpenRead(path);
            using Microsoft.Maui.Graphics.IImage original = PlatformImage.FromStream(input);
            using Microsoft.Maui.Graphics.IImage resized = original.Downsize(MaxLongestSide);

            var target = Path.Combine(Path.GetDirectoryName(path)!, $"{Guid.NewGuid():N}.jpg");
            using (var output = File.Create(target))
                resized.Save(output, ImageFormat.Jpeg, JpegQuality);

            input.Close();
            File.Delete(path);
            return target;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Photos] Could not resize {Path.GetFileName(path)}: {ex.Message}");
            return path;
        }
    }
}
