using DryCleaning.Models;

namespace DryCleaning.Services;

public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
}
