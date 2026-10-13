using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using ViewState = DryCleaning.ViewModels.ViewState;

namespace DryCleaning.Controls;

/// <summary>Horizontally scrolling chip tabs bound to a collection of <see cref="ViewModels.Items.SegmentItem"/>.</summary>
public sealed class SegmentBar : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(SegmentBar), null,
        propertyChanged: (bindable, oldValue, newValue) => ((SegmentBar)bindable).OnItemsSourceChanged(oldValue as IEnumerable, newValue as IEnumerable));

    private readonly HorizontalStackLayout _stack = new() { Spacing = 8, Padding = new Thickness(16, 12) };
    private readonly List<ViewModels.Items.SegmentItem> _subscribed = [];

    public SegmentBar()
    {
        BackgroundColor = Theme.Card;
        Content = new VerticalStackLayout
        {
            Children =
            {
                new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _stack },
                new BoxView { HeightRequest = 1, Color = Theme.Border }
            }
        };
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
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
        foreach (var item in _subscribed)
            item.PropertyChanged -= OnItemPropertyChanged;
        _subscribed.Clear();
        _stack.Children.Clear();

        if (ItemsSource is null)
            return;

        foreach (var obj in ItemsSource)
        {
            if (obj is not ViewModels.Items.SegmentItem item)
                continue;

            var label = new Label { FontSize = 14, FontFamily = "AppSemibold", VerticalOptions = LayoutOptions.Center };
            var chip = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                StrokeThickness = 1,
                Padding = new Thickness(16, 8),
                Content = label,
                BindingContext = item
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => item.SelectCommand.Execute(null);
            chip.GestureRecognizers.Add(tap);

            Apply(chip, label, item);
            item.PropertyChanged += OnItemPropertyChanged;
            _subscribed.Add(item);
            _stack.Children.Add(chip);
        }
    }

    private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not ViewModels.Items.SegmentItem item)
            return;

        foreach (var child in _stack.Children)
        {
            if (child is Border { BindingContext: ViewModels.Items.SegmentItem bound } chip && ReferenceEquals(bound, item) && chip.Content is Label label)
                Apply(chip, label, item);
        }
    }

    private static void Apply(Border chip, Label label, ViewModels.Items.SegmentItem item)
    {
        label.Text = item.HasCount ? $"{item.Title}  {item.Count}" : item.Title;
        label.TextColor = item.IsSelected ? Colors.White : Theme.MutedForeground;
        chip.BackgroundColor = item.IsSelected ? Theme.Primary : Theme.Card;
        chip.Stroke = new SolidColorBrush(item.IsSelected ? Theme.Primary : Theme.Border);
        SemanticProperties.SetDescription(chip, item.IsSelected ? $"{item.Title}, selected" : item.Title);
    }
}
