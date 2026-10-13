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
