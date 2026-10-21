using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.Services;

public sealed class DocumentPicker(IDialogService dialogs, IPhotoService photos) : IDocumentPicker
{
    private const string Photo = "Photo (camera or gallery)";
    private const string Pdf = "PDF file";

    public async Task<PickedDocument?> PickAsync(long maxPdfBytes, long maxImageBytes)
    {
        var choice = await dialogs.ActionSheetAsync("Add document", "Cancel", [Photo, Pdf]);
        if (choice is null)
            return null;

        if (choice == Photo)
        {
            var path = await photos.AddDocumentPhotoAsync(maxImageBytes);
            return path is null ? null : new PickedDocument { Path = path, FileName = Path.GetFileName(path) };
        }

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Choose a PDF",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    [DevicePlatform.Android] = ["application/pdf"],
                    [DevicePlatform.iOS] = ["com.adobe.pdf"]
                })
            });
            if (result is null)
                return null;

            // Copy out of the picker's temporary location, so the upload can read it later.
            // A unique folder per pick keeps the original file name (the admin sees it), without clashing with earlier picks.
            var folder = System.IO.Path.Combine(FileSystem.CacheDirectory, "documents", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var fileName = string.IsNullOrWhiteSpace(result.FileName) ? "document.pdf" : System.IO.Path.GetFileName(result.FileName);
            var target = System.IO.Path.Combine(folder, fileName);
            await using (var source = await result.OpenReadAsync())
            await using (var file = File.Create(target))
                await source.CopyToAsync(file);

            if (new FileInfo(target).Length > maxPdfBytes)
            {
                File.Delete(target);
                await dialogs.AlertAsync("File too large", $"Please choose a PDF smaller than {SizeText.Megabytes(maxPdfBytes)}.");
                return null;
            }

            return new PickedDocument { Path = target, FileName = fileName };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await dialogs.AlertAsync("Couldn't open the file", ex.Message);
            return null;
        }
    }
}
