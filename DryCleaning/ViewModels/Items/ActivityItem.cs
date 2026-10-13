using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

public sealed class ActivityItem
{
    public ActivityItem(ActivityEntry entry, Func<Guid, Task> open)
    {
        Entry = entry;
        OpenCommand = new AsyncRelayCommand(() => open(entry.OrderId));
    }

    public ActivityEntry Entry { get; }
    public string Text => Entry.Text;
    public string TimeText => TimeFormat.Relative(Entry.At);
    public OrderStatus Status => Entry.Status;
    public IAsyncRelayCommand OpenCommand { get; }
}
