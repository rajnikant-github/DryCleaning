using DryCleaning.Admin.Services;
using Microsoft.AspNetCore.Components;

namespace DryCleaning.Admin.Components;

/// <summary>Shared plumbing for admin pages: the API client plus busy / error / success state around calls.</summary>
public abstract class AdminPageBase : ComponentBase
{
    [Inject] protected AdminApiClient Api { get; set; } = default!;

    protected bool Busy { get; private set; }
    protected string? Error { get; private set; }
    protected string? Success { get; private set; }
    protected bool SessionExpired { get; private set; }

    /// <summary>Runs an API action, showing its error (or the success text) instead of crashing the page.</summary>
    protected async Task<bool> RunAsync(Func<Task> action, string? successMessage = null)
    {
        Busy = true;
        Error = null;
        Success = null;
        SessionExpired = false;
        StateHasChanged();
        try
        {
            await action();
            Success = successMessage;
            return true;
        }
        catch (ApiException ex)
        {
            Error = ex.Message;
            SessionExpired = ex.IsUnauthorized;
            return false;
        }
        finally
        {
            Busy = false;
            StateHasChanged();
        }
    }

    protected void ClearMessages()
    {
        Error = null;
        Success = null;
    }
}
