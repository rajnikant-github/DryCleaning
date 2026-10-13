using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

public sealed partial class SegmentItem : ObservableObject
{
    public SegmentItem(string key, string title, Action<SegmentItem> onSelected)
    {
        Key = key;
        Title = title;
        SelectCommand = new RelayCommand(() => onSelected(this));
    }

    public string Key { get; }
    public string Title { get; }
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCount))]
    private int count;

    public bool HasCount => Count > 0;
}
