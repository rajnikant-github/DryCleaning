using DryCleaning.Models;

namespace DryCleaning.Services;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
    {
        var shell = Shell.Current;
        if (shell is null)
            return Task.CompletedTask;

        return parameters is null
            ? shell.GoToAsync(route)
            : shell.GoToAsync(route, parameters);
    }

    public Task GoBackAsync() => GoToAsync("..");
}

public sealed class DialogService : IDialogService
{
    private static Page? CurrentPage =>
        Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;

    public async Task AlertAsync(string title, string message, string cancel = "OK")
    {
        var page = CurrentPage;
        if (page is not null)
            await page.DisplayAlert(title, message, cancel);
    }

    public async Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "Cancel")
    {
        var page = CurrentPage;
        return page is not null && await page.DisplayAlert(title, message, accept, cancel);
    }

    public async Task<string?> PromptAsync(string title, string message, string accept = "Submit", string placeholder = "", string initialValue = "")
    {
        var page = CurrentPage;
        if (page is null)
            return null;

        return await page.DisplayPromptAsync(title, message, accept, "Cancel", placeholder, -1, Keyboard.Text, initialValue);
    }

    public async Task<string?> ActionSheetAsync(string title, string cancel, params string[] buttons)
    {
        var page = CurrentPage;
        if (page is null)
            return null;

        var result = await page.DisplayActionSheet(title, cancel, null, buttons);
        return result == cancel ? null : result;
    }
}

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

public sealed class SessionService : ISessionService
{
    public UserRole SelectedRole { get; set; } = UserRole.Customer;
    public User? CurrentUser { get; private set; }
    public Guid? ProfileId { get; private set; }
    public Guid? ProviderId => CurrentUser?.Role == UserRole.Provider ? ProfileId : null;
    public Guid? CustomerId => CurrentUser?.Role == UserRole.Customer ? ProfileId : null;
    public bool IsSignedIn => CurrentUser is not null;

    public void SignIn(User user, Guid? profileId)
    {
        CurrentUser = user;
        ProfileId = profileId;
        SelectedRole = user.Role;
    }

    public void SignOut()
    {
        CurrentUser = null;
        ProfileId = null;
    }
}
