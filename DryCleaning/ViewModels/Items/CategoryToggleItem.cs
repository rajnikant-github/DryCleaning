using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;

namespace DryCleaning.ViewModels.Items;

/// <summary>A category the provider can switch on or off (sign-up and "Categories I offer").</summary>
public sealed partial class CategoryToggleItem : ObservableObject
{
    public CategoryToggleItem(Guid id, string name, string description, bool isSelected)
    {
        Id = id;
        Name = name;
        Description = description;
        IsSelected = isSelected;
        ToggleCommand = new RelayCommand(() => IsSelected = !IsSelected);
    }

    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
    public string Icon => CatalogIcons.ForCategory(Name);
    public IRelayCommand ToggleCommand { get; }

    [ObservableProperty]
    private bool isSelected;
}
