using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Configuration;
using DryCleaning.Services;

namespace DryCleaning.ViewModels;

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
