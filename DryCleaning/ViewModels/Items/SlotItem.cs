using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Items;

public sealed partial class SlotItem : ObservableObject
{
    public SlotItem(string label, bool isAvailable, Action<SlotItem> select)
    {
        Label = label;
        IsAvailable = isAvailable;
        SelectCommand = new RelayCommand(() => { if (IsAvailable) select(this); });
    }

    public string Label { get; }
    public bool IsAvailable { get; }
    public bool IsUnavailable => !IsAvailable;
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool isSelected;
}
