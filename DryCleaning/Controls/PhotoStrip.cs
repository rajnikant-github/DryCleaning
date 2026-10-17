using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using ViewState = DryCleaning.ViewModels.ViewState;

namespace DryCleaning.Controls;

/// <summary>Horizontal list of photo thumbnails with an "add photo" tile. Removes on tap of ×.</summary>
public sealed class PhotoStrip : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(PhotoStrip), null,
        propertyChanged: (bindable, oldValue, newValue) => ((PhotoStrip)bindable).OnItemsSourceChanged(oldValue as IEnumerable, newValue as IEnumerable));

    public static readonly BindableProperty AddCommandProperty =
        BindableProperty.Create(nameof(AddCommand), typeof(ICommand), typeof(PhotoStrip));

    public static readonly BindableProperty AddTextProperty = BindableProperty.Create(
        nameof(AddText), typeof(string), typeof(PhotoStrip), "Add photo",
        propertyChanged: (bindable, _, _) => ((PhotoStrip)bindable).Rebuild());

    public static readonly BindableProperty AllowEditingProperty = BindableProperty.Create(
        nameof(AllowEditing), typeof(bool), typeof(PhotoStrip), true,
        propertyChanged: (bindable, _, _) => ((PhotoStrip)bindable).Rebuild());

    private readonly HorizontalStackLayout _stack = new() { Spacing = 10 };

    public PhotoStrip()
    {
        Content = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _stack };
        Rebuild();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? AddCommand
    {
        get => (ICommand?)GetValue(AddCommandProperty);
        set => SetValue(AddCommandProperty, value);
    }

    public string AddText
    {
        get => (string)GetValue(AddTextProperty);
        set => SetValue(AddTextProperty, value);
    }

    /// <summary>When false the strip is read-only: no add tile and no remove buttons.</summary>
    public bool AllowEditing
    {
        get => (bool)GetValue(AllowEditingProperty);
        set => SetValue(AllowEditingProperty, value);
    }

    private void OnItemsSourceChanged(IEnumerable? oldValue, IEnumerable? newValue)
    {
        if (oldValue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= OnCollectionChanged;
        if (newValue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += OnCollectionChanged;
        Rebuild();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        _stack.Children.Clear();

        var addTile = new Border
        {
            WidthRequest = 96,
            HeightRequest = 96,
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            Stroke = new SolidColorBrush(Theme.Border),
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            BackgroundColor = Theme.Card,
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = AppIcons.Camera, FontFamily = AppIcons.Family, FontSize = 22, TextColor = Theme.MutedForeground, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = AddText, FontSize = 12, TextColor = Theme.MutedForeground, HorizontalOptions = LayoutOptions.Center }
                }
            }
        };
        var addTap = new TapGestureRecognizer();
        addTap.Tapped += (_, _) =>
        {
            if (AddCommand?.CanExecute(null) == true)
                AddCommand.Execute(null);
        };
        addTile.GestureRecognizers.Add(addTap);
        SemanticProperties.SetDescription(addTile, AddText);
        if (AllowEditing)
            _stack.Children.Add(addTile);

        if (ItemsSource is null)
            return;

        foreach (var item in ItemsSource)
        {
            if (item is not string path)
                continue;

            var remove = new Border
            {
                WidthRequest = 24,
                HeightRequest = 24,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Color.FromArgb("#99000000"),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 4, 4, 0),
                Content = new Label { Text = "×", TextColor = Colors.White, FontSize = 16, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
            };
            var removeTap = new TapGestureRecognizer();
            removeTap.Tapped += (_, _) =>
            {
                if (ItemsSource is IList list && !list.IsReadOnly)
                    list.Remove(path);
            };
            remove.GestureRecognizers.Add(removeTap);
            SemanticProperties.SetDescription(remove, "Remove photo");

            var tile = new Border
            {
                WidthRequest = 96,
                HeightRequest = 96,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Content = new Grid
                {
                    Children =
                    {
                        new Image { Source = Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http") ? ImageSource.FromUri(uri) : ImageSource.FromFile(path), Aspect = Aspect.AspectFill }
                    }
                }
            };
            if (AllowEditing && tile.Content is Grid tileGrid)
                tileGrid.Children.Add(remove);
            _stack.Children.Add(tile);
        }
    }
}
