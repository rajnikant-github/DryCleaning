using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IDialogService
{
    Task AlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "Cancel");
    Task<string?> PromptAsync(string title, string message, string accept = "Submit", string placeholder = "", string initialValue = "");
    Task<string?> ActionSheetAsync(string title, string cancel, params string[] buttons);
}
