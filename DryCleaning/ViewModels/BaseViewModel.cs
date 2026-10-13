using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Configuration;
using DryCleaning.Services;

namespace DryCleaning.ViewModels;

public enum ViewState
{
    Loading,
    Content,
    Empty,
    Error,
    Offline
}

/// <summary>Common services every ViewModel needs, bundled to keep constructors short.</summary>
public sealed class ViewModelServices(
    INavigationService navigation,
    IDialogService dialogs,
    IConnectivity connectivity,
    AppSettings settings)
{
    public INavigationService Navigation { get; } = navigation;
    public IDialogService Dialogs { get; } = dialogs;
    public IConnectivity Connectivity { get; } = connectivity;
    public AppSettings Settings { get; } = settings;
}

public abstract partial class BaseViewModel : ObservableObject
{
    private bool _hasLoaded;

    protected BaseViewModel(ViewModelServices services)
    {
        Services = services;
    }

    protected ViewModelServices Services { get; }
    protected INavigationService Navigation => Services.Navigation;
    protected IDialogService Dialogs => Services.Dialogs;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
    [NotifyPropertyChangedFor(nameof(IsContent))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    [NotifyPropertyChangedFor(nameof(IsOffline))]
    private ViewState state = ViewState.Loading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string errorMessage = "Something went wrong";

    public bool IsLoading => State == ViewState.Loading;
    public bool IsContent => State == ViewState.Content;
    public bool IsEmpty => State == ViewState.Empty;
    public bool IsError => State == ViewState.Error;
    public bool IsOffline => State == ViewState.Offline;
    public bool IsNotBusy => !IsBusy;

    /// <summary>Called by <see cref="Views.BasePage"/> every time the page appears.</summary>
    public virtual Task OnAppearingAsync() => LoadAsync(showSkeleton: !_hasLoaded);

    /// <summary>Load the page data. Return false when there is nothing to show (empty state).</summary>
    protected virtual Task<bool> LoadDataAsync() => Task.FromResult(true);

    [RelayCommand]
    private Task RetryAsync() => LoadAsync(showSkeleton: true);

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync(showSkeleton: false);
        IsRefreshing = false;
    }

    protected async Task LoadAsync(bool showSkeleton)
    {
        if (!Services.Settings.Features.UseMockData &&
            Services.Connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            State = ViewState.Offline;
            return;
        }

        if (showSkeleton)
            State = ViewState.Loading;

        try
        {
            var hasContent = await LoadDataAsync();
            State = hasContent ? ViewState.Content : ViewState.Empty;
            _hasLoaded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            State = ViewState.Error;
        }
    }

    /// <summary>Runs a user action with a busy flag and friendly error handling.</summary>
    protected async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync("Something went wrong", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected static Dictionary<string, object> Param(string key, object value) => new() { [key] = value };

    protected static bool TryGetGuid(IDictionary<string, object> query, string key, out Guid id)
    {
        id = Guid.Empty;
        if (!query.TryGetValue(key, out var value))
            return false;

        switch (value)
        {
            case Guid guid:
                id = guid;
                return true;
            case string text when Guid.TryParse(text, out var parsed):
                id = parsed;
                return true;
            default:
                return false;
        }
    }
}
