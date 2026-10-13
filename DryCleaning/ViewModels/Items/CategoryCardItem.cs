using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Items;

// Item ViewModels for the Customer screens (each carries its own commands).

public sealed class CategoryCardItem(Category category, Func<Category, Task> open)
{
    public Guid Id => category.Id;
    public string Name => category.Name;
    public string Description => category.Description;
    public string Icon => CatalogIcons.ForCategory(category.Name);
    public IAsyncRelayCommand OpenCommand { get; } = new AsyncRelayCommand(() => open(category));
}
