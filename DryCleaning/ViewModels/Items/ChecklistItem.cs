using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

public sealed partial class ChecklistItem : ObservableObject
{
    private readonly Action? _changed;

    public ChecklistItem(string text, Action? changed = null)
    {
        Text = text;
        _changed = changed;
        ToggleCommand = new RelayCommand(() => IsChecked = !IsChecked);
    }

    public string Text { get; }
    public IRelayCommand ToggleCommand { get; }

    [ObservableProperty]
    private bool isChecked;

    partial void OnIsCheckedChanged(bool value) => _changed?.Invoke();
}
