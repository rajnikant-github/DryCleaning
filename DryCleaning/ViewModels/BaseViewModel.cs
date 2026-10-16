using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DryCleaning.Configuration;
using DryCleaning.Services;
using DryCleaning.Services.Api;
using DryCleaning.Services.Realtime;

namespace DryCleaning.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    private static readonly TimeSpan RealtimeDebounce = TimeSpan.FromMilliseconds(500);

    private readonly SemaphoreSlim _realtimeGate = new(1, 1);
    private CancellationTokenSource? _realtimeDelay;
    private bool _realtimeAttached;
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

    /// <summary>
    /// Return true when this screen should quietly reload for a live update from the API.
    /// Screens opt in by overriding; forms and multi-step flows must not (a reload would overwrite what the user typed).
    /// </summary>
    protected virtual bool ReactsTo(RealtimeChangedMessage message) => false;

    /// <summary>Called by <see cref="Views.BasePage"/> when the page appears: start listening for live updates.</summary>
    public void AttachRealtime()
    {
        if (_realtimeAttached)
            return;
        _realtimeAttached = true;
        WeakReferenceMessenger.Default.Register<BaseViewModel, RealtimeChangedMessage>(this, static (viewModel, message) => viewModel.OnRealtimeChange(message));
    }

    /// <summary>Called by <see cref="Views.BasePage"/> when the page disappears: stop listening.</summary>
    public void DetachRealtime()
    {
        if (!_realtimeAttached)
            return;
        _realtimeAttached = false;
        WeakReferenceMessenger.Default.Unregister<RealtimeChangedMessage>(this);
        _realtimeDelay?.Cancel();
    }

    private void OnRealtimeChange(RealtimeChangedMessage message)
    {
        if (!ReactsTo(message))
            return;

        // Several events often arrive together (order + job + notification) – reload once.
        _realtimeDelay?.Cancel();
        var delay = _realtimeDelay = new CancellationTokenSource();
        _ = ReloadQuietlyAsync(delay.Token);
    }

    private async Task ReloadQuietlyAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(RealtimeDebounce, ct);
            await _realtimeGate.WaitAsync(ct);
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Not loaded yet, or the user is in the middle of an action: the normal load / the action's own reload covers it.
                    if (!_hasLoaded || IsBusy || Services.Connectivity.NetworkAccess != NetworkAccess.Internet)
                        return;
                    try
                    {
                        var hasContent = await LoadDataAsync();
                        State = hasContent ? ViewState.Content : ViewState.Empty;
                    }
                    catch (ApiException ex) when (ex.IsNotFound)
                    {
                        // The thing on screen is gone (e.g. another driver took the job): don't leave stale buttons behind.
                        ErrorMessage = ex.Message;
                        State = ViewState.Error;
                    }
                });
            }
            finally
            {
                _realtimeGate.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // A newer update replaced this one, or the page disappeared.
        }
        catch (Exception ex)
        {
            // Quiet by design: keep showing what is on screen instead of flipping to an error.
            System.Diagnostics.Debug.WriteLine($"[{GetType().Name}] Live reload failed: {ex.Message}");
        }
    }

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
