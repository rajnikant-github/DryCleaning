using DryCleaning.Models;

namespace DryCleaning.Services;

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
