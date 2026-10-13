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
