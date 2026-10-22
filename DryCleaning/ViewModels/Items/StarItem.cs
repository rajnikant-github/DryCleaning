using CommunityToolkit.Mvvm.ComponentModel;

namespace DryCleaning.ViewModels.Items;

/// <summary>One star in a 1 to 5 star row.</summary>
public sealed partial class StarItem(int number) : ObservableObject
{
    public int Number { get; } = number;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StarColor))]
    private bool isFilled;

    public Color StarColor => IsFilled ? Color.FromArgb("#F59E0B") : Color.FromArgb("#D1D5DB");
}
