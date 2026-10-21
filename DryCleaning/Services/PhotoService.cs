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

    // Photos of documents need to stay readable: bigger and sharper than a normal order photo.
    private const float DocumentLongestSide = 2600f;
    private const float DocumentJpegQuality = 0.85f;
    private const float SmallestDocumentSide = 1400f;

    public Task<string?> AddPhotoAsync() => PickAsync(MaxLongestSide, JpegQuality, maxBytes: null);

    public Task<string?> AddDocumentPhotoAsync(long maxBytes) => PickAsync(DocumentLongestSide, DocumentJpegQuality, maxBytes);

    private async Task<string?> PickAsync(float maxLongestSide, float jpegQuality, long? maxBytes)
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

            return await Task.Run(() => Shrink(target, maxLongestSide, jpegQuality, maxBytes));
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
    private static string Shrink(string path, float maxLongestSide, float jpegQuality, long? maxBytes)
    {
        try
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            var length = new FileInfo(path).Length;
            if (length < SkipResizeBelowBytes && extension is ".jpg" or ".jpeg")
                return path;

            using var input = File.OpenRead(path);
            using Microsoft.Maui.Graphics.IImage original = PlatformImage.FromStream(input);

            var target = Path.Combine(Path.GetDirectoryName(path)!, $"{Guid.NewGuid():N}.jpg");
            var side = maxLongestSide;
            var quality = jpegQuality;
            while (true)
            {
                using (Microsoft.Maui.Graphics.IImage resized = original.Downsize(side))
                using (var output = File.Create(target))
                    resized.Save(output, ImageFormat.Jpeg, quality);

                // Still too big for the server? Lower the quality a little, then the size, never below a readable size.
                if (maxBytes is null || new FileInfo(target).Length <= maxBytes || (quality <= 0.5f && side <= SmallestDocumentSide))
                    break;
                if (quality > 0.5f)
                    quality = Math.Max(0.5f, quality - 0.1f);
                else
                    side = Math.Max(SmallestDocumentSide, side * 0.8f);
            }

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
